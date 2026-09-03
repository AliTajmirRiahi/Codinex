using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Tools.BuiltIn.Files;
using Codinex.Core.Interfaces.Workspace;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.VisualStudio.Tools.Files;

[TestFixture]
public sealed class ReadFileToolTests
{
    private const string RequestedPath = "views/chatView.js";
    private const string FileName = "chatView.js";
    private const string RelativePath = @"src\Codinex.UI\views\chatView.js";
    private const string FullPath = @"C:\ws\src\Codinex.UI\views\chatView.js";

    private IWorkspaceFileService _files = null!;
    private IWorkspaceSearchService _search = null!;

    [SetUp]
    public void SetUp()
    {
        _files = Substitute.For<IWorkspaceFileService>();
        _search = Substitute.For<IWorkspaceSearchService>();
    }

    private ReadFileTool CreateSut() => new(_files, _search);

    // ----------------------------------------------------------------- helpers

    private static ToolRequest Request(string path, int? startLine = null, int? endLine = null)
    {
        var args = new JObject();

        if (path != null)
        {
            args["path"] = path;
        }

        if (startLine.HasValue)
        {
            args["startLine"] = startLine.Value;
        }

        if (endLine.HasValue)
        {
            args["endLine"] = endLine.Value;
        }

        return new ToolRequest { Id = "call-1", Name = "read_file", Arguments = args };
    }

    private void SingleFileFound(string content)
    {
        _search.FindFiles(RequestedPath).Returns(new List<WorkspaceFile>
        {
            new() { Name = FileName, RelativePath = RelativePath, FullPath = FullPath }
        });

        _files.ReadAsync(FullPath, Arg.Any<CancellationToken>()).Returns(content);
    }

    private static JObject Payload(ToolResult result) => JObject.FromObject(result.Data);

    private static string JoinLines(int count, string text = "line") =>
        string.Join("\n", Enumerable.Range(1, count).Select(i => $"{text} {i}"));

    private static string LinesOfWidth(int count, int width) =>
        string.Join("\n", Enumerable.Repeat(new string('x', Math.Max(1, width)), count));

    // ----------------------------------------------------- path resolution

    [Test]
    public async Task ExecuteAsync_WhenNoFileMatches_ReturnsFailure()
    {
        _search.FindFiles(RequestedPath).Returns(new List<WorkspaceFile>());

        var result = await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No file matching");
        await _files.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenMultipleFilesMatch_ReturnsMatchListWithoutReadingContent()
    {
        _search.FindFiles(RequestedPath).Returns(new List<WorkspaceFile>
        {
            new() { Name = "chatView.js", RelativePath = @"a\chatView.js", FullPath = @"C:\a\chatView.js" },
            new() { Name = "chatView.js", RelativePath = @"b\chatView.js", FullPath = @"C:\b\chatView.js" }
        });

        var result = await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None);

        result.Success.Should().BeTrue();

        var matches = (JArray)Payload(result)["matches"]!;
        matches.Should().HaveCount(2);
        matches[0]["RelativePath"]!.Value<string>().Should().Be(@"a\chatView.js");
        Payload(result)["Content"].Should().BeNull();

        await _files.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenPathArgumentMissing_Throws()
    {
        var act = () => CreateSut().ExecuteAsync(Request(path: null), CancellationToken.None);

        await act.Should().ThrowAsync<ToolRequestValidationException>();
    }

    // ------------------------------------------------- whole (small) file

    [Test]
    public async Task ExecuteAsync_SmallFile_ReturnsWholeContentByteForByte()
    {
        const string content = "line 1\r\nline 2\r\nline 3";
        SingleFileFound(content);

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["Content"]!.Value<string>().Should().Be(content);
        payload["StartLine"]!.Value<int>().Should().Be(1);
        payload["EndLine"]!.Value<int>().Should().Be(3);
        payload["TotalLines"]!.Value<int>().Should().Be(3);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
        payload["Note"]!.Type.Should().Be(JTokenType.Null);
    }

    [Test]
    public async Task ExecuteAsync_SmallFile_EchoesFileNameAndRelativePath()
    {
        SingleFileFound("x");

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["Name"]!.Value<string>().Should().Be(FileName);
        payload["RelativePath"]!.Value<string>().Should().Be(RelativePath);
    }

    [Test]
    public async Task ExecuteAsync_EmptyFile_ReturnsEmptyContentAndOneLine()
    {
        SingleFileFound(string.Empty);

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["Content"]!.Value<string>().Should().Be(string.Empty);
        payload["TotalLines"]!.Value<int>().Should().Be(1);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenServiceReturnsNull_TreatsContentAsEmpty()
    {
        SingleFileFound(null);

        var result = await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None);

        result.Success.Should().BeTrue();
        Payload(result)["Content"]!.Value<string>().Should().Be(string.Empty);
    }

    [Test]
    public async Task ExecuteAsync_CountsLinesAfterNormalizingCrlf()
    {
        SingleFileFound("a\r\nb\nc\r\nd");

        Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None))
            ["TotalLines"]!.Value<int>().Should().Be(4);
    }

    // ------------------------------------------ large file, range-less read

    [Test]
    public async Task ExecuteAsync_LargeFile_TruncatesToHeadAndFlagsIt()
    {
        // 600 lines * ~41 chars ≈ 24.6k chars -> over the 20k untruncated cap.
        SingleFileFound(LinesOfWidth(600, 40));

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["TotalLines"]!.Value<int>().Should().Be(600);
        payload["StartLine"]!.Value<int>().Should().Be(1);
        payload["EndLine"]!.Value<int>().Should().BeLessThan(600).And.BePositive();
        payload["IsTruncated"]!.Value<bool>().Should().BeTrue();
        payload["Note"]!.Value<string>().Should().Contain("File is large");

        var returnedLines = payload["Content"]!.Value<string>().Split('\n').Length;
        returnedLines.Should().Be(payload["EndLine"]!.Value<int>());
    }

    [Test]
    public async Task ExecuteAsync_LargeFile_HeadIsCappedByCharBudget()
    {
        // Wide lines: the ~20k char budget runs out well before 400 lines.
        SingleFileFound(LinesOfWidth(200, 250));

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["Content"]!.Value<string>().Length.Should().BeLessThanOrEqualTo(20_000);
        payload["EndLine"]!.Value<int>().Should().BeLessThan(400);
        payload["IsTruncated"]!.Value<bool>().Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_LargeFile_HeadIsCappedByLineCount()
    {
        // Narrow lines: 400-line cap is hit before the char budget.
        SingleFileFound(LinesOfWidth(600, 40));

        Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None))
            ["EndLine"]!.Value<int>().Should().Be(400);
    }

    [Test]
    public async Task ExecuteAsync_SingleHugeLineWithNoNewlines_StillReturnsThatLine()
    {
        var hugeLine = new string('y', 30_000);
        SingleFileFound(hugeLine);

        var payload = Payload(await CreateSut().ExecuteAsync(Request(RequestedPath), CancellationToken.None));

        payload["TotalLines"]!.Value<int>().Should().Be(1);
        payload["EndLine"]!.Value<int>().Should().Be(1);
        payload["Content"]!.Value<string>().Should().Be(hugeLine);
        payload["Note"]!.Value<string>().Should().Contain("File is large");
    }

    // ---------------------------------------------------------- range mode

    [Test]
    public async Task ExecuteAsync_WithStartAndEndLine_ReturnsExactInclusiveSlice()
    {
        SingleFileFound(JoinLines(100));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 10, endLine: 20), CancellationToken.None));

        payload["StartLine"]!.Value<int>().Should().Be(10);
        payload["EndLine"]!.Value<int>().Should().Be(20);
        payload["TotalLines"]!.Value<int>().Should().Be(100);
        payload["IsTruncated"]!.Value<bool>().Should().BeTrue();

        var content = payload["Content"]!.Value<string>();
        content.Split('\n').Should().HaveCount(11);
        content.Split('\n').First().Should().Be("line 10");
        content.Split('\n').Last().Should().Be("line 20");
    }

    [Test]
    public async Task ExecuteAsync_WithOnlyStartLine_ReadsToEndOfFile()
    {
        SingleFileFound(JoinLines(100));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 96), CancellationToken.None));

        payload["StartLine"]!.Value<int>().Should().Be(96);
        payload["EndLine"]!.Value<int>().Should().Be(100);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
        payload["Content"]!.Value<string>().Split('\n').Should().HaveCount(5);
    }

    [Test]
    public async Task ExecuteAsync_WithOnlyEndLine_ReadsFromFirstLine()
    {
        SingleFileFound(JoinLines(100));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, endLine: 5), CancellationToken.None));

        payload["StartLine"]!.Value<int>().Should().Be(1);
        payload["EndLine"]!.Value<int>().Should().Be(5);
        payload["Content"]!.Value<string>().Split('\n').First().Should().Be("line 1");
    }

    [Test]
    public async Task ExecuteAsync_RangeRequest_BypassesLargeFileTruncation()
    {
        // A large file, but an explicit 1..500 range must be honoured in full.
        SingleFileFound(LinesOfWidth(600, 40));

        Payload(await CreateSut()
                .ExecuteAsync(Request(RequestedPath, startLine: 1, endLine: 500), CancellationToken.None))
            ["EndLine"]!.Value<int>().Should().Be(500);
    }

    [Test]
    public async Task ExecuteAsync_StartLinePastEndOfFile_ReturnsEmptyWithNote()
    {
        SingleFileFound(JoinLines(10));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 50), CancellationToken.None));

        payload["Content"]!.Value<string>().Should().BeEmpty();
        payload["StartLine"]!.Value<int>().Should().Be(0);
        payload["EndLine"]!.Value<int>().Should().Be(0);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
        payload["Note"]!.Value<string>().Should().Contain("past the end");
    }

    [Test]
    public async Task ExecuteAsync_EndLineBeyondFile_ClampsToTotalLines()
    {
        SingleFileFound(JoinLines(10));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 5, endLine: 999), CancellationToken.None));

        payload["EndLine"]!.Value<int>().Should().Be(10);
        payload["Content"]!.Value<string>().Split('\n').Should().HaveCount(6);
    }

    [Test]
    public async Task ExecuteAsync_EndLineBeforeStartLine_ClampsToStartLine()
    {
        SingleFileFound(JoinLines(50));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 20, endLine: 5), CancellationToken.None));

        payload["StartLine"]!.Value<int>().Should().Be(20);
        payload["EndLine"]!.Value<int>().Should().Be(20);
        payload["Content"]!.Value<string>().Should().Be("line 20");
    }

    [Test]
    public async Task ExecuteAsync_RangeOnSmallFile_StillHonoursRange()
    {
        SingleFileFound(JoinLines(6));

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request(RequestedPath, startLine: 2, endLine: 3), CancellationToken.None));

        payload["Content"]!.Value<string>().Should().Be("line 2\nline 3");
    }

    // ------------------------------------------------------- cancellation

    [Test]
    public async Task ExecuteAsync_ForwardsCancellationTokenToReadAsync()
    {
        SingleFileFound("x");
        using var cts = new CancellationTokenSource();

        await CreateSut().ExecuteAsync(Request(RequestedPath), cts.Token);

        await _files.Received(1).ReadAsync(FullPath, cts.Token);
    }
}
