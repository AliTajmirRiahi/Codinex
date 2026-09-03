using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Models.Tools.SearchProject;
using Codinex.VisualStudio.Tools.BuiltIn.Search;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.VisualStudio.Tools.Search;

[TestFixture]
public sealed class SearchProjectToolTests
{
    private IWorkspaceSearchService _search = null!;

    [SetUp]
    public void SetUp() => _search = Substitute.For<IWorkspaceSearchService>();

    private SearchProjectTool CreateSut() => new(_search);

    // --------------------------------------------------------------- helpers

    private static ToolRequest Request(string query, string type, int? skip = null, int? take = null)
    {
        var args = new JObject();

        if (query != null)
        {
            args["query"] = query;
        }

        if (type != null)
        {
            args["type"] = type;
        }

        if (skip.HasValue)
        {
            args["skip"] = skip.Value;
        }

        if (take.HasValue)
        {
            args["take"] = take.Value;
        }

        return new ToolRequest { Id = "call-1", Name = "search_project", Arguments = args };
    }

    private static WorkspaceFile File(
        string name = "file.cs",
        string rel = @"src\file.cs",
        int line = 1,
        int col = 1,
        string preview = "match")
        => new()
        {
            Name = name,
            RelativePath = rel,
            FullPath = @"C:\ws\" + rel,
            LineNumber = line,
            Column = col,
            Preview = preview,
            MatchType = WorkspaceMatchType.Content
        };

    private static WorkspaceFile[] Files(int count) =>
        Enumerable.Range(1, count)
            .Select(i => File(name: $"f{i}.cs", rel: $@"src\f{i}.cs", line: i))
            .ToArray();

    private void SearchReturns(SearchProjectType type, params WorkspaceFile[] files) =>
        _search.Search(Arg.Any<string>(), type).Returns(files.ToList());

    private static JObject Payload(ToolResult result) => JObject.FromObject(result.Data);

    private static JArray Results(ToolResult result) => (JArray)Payload(result)["Results"]!;

    // ----------------------------------------------------- guard / parsing

    [Test]
    public async Task ExecuteAsync_WhenQueryMissing_ReturnsFailure()
    {
        // The tool wraps its whole body in try/catch, so the missing-argument exception
        // surfaces as a failed ToolResult rather than being thrown.
        var result = await CreateSut().ExecuteAsync(Request(query: null, type: "text"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("query");
    }

    [Test]
    public async Task ExecuteAsync_WhenTypeMissing_ReturnsFailure()
    {
        var result = await CreateSut().ExecuteAsync(Request(query: "x", type: null), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("type");
    }

    [Test]
    public async Task ExecuteAsync_WhenTypeUnknown_ReturnsFailure()
    {
        var result = await CreateSut().ExecuteAsync(Request("x", "bogus"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unsupported search type 'bogus'");
    }

    [Test]
    public async Task ExecuteAsync_TypeParsingIsCaseInsensitive()
    {
        SearchReturns(SearchProjectType.Text, File());

        var result = await CreateSut().ExecuteAsync(Request("x", "TEXT"), CancellationToken.None);

        result.Success.Should().BeTrue();
        Payload(result)["Type"]!.Value<string>().Should().Be("TEXT"); // echoed verbatim
    }

    [Test]
    public async Task ExecuteAsync_WhenTokenAlreadyCancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await CreateSut().ExecuteAsync(Request("x", "text"), cts.Token);

        result.Success.Should().BeFalse();
    }

    // ------------------------------------------------------ normal search

    [Test]
    public async Task ExecuteAsync_MapsResultRows()
    {
        SearchReturns(SearchProjectType.Text, File(name: "chatView.js", rel: @"src\chatView.js", line: 42, col: 7, preview: "const x"));

        var rows = Results(await CreateSut().ExecuteAsync(Request("x", "text"), CancellationToken.None));

        rows.Should().HaveCount(1);
        rows[0]["Name"]!.Value<string>().Should().Be("chatView.js");
        rows[0]["RelativePath"]!.Value<string>().Should().Be(@"src\chatView.js");
        rows[0]["LineNumber"]!.Value<int>().Should().Be(42);
        rows[0]["Column"]!.Value<int>().Should().Be(7);
        rows[0]["Preview"]!.Value<string>().Should().Be("const x");
    }

    [Test]
    public async Task ExecuteAsync_ReportsCountsAndNoNote()
    {
        SearchReturns(SearchProjectType.Text, Files(3));

        var payload = Payload(await CreateSut().ExecuteAsync(Request("x", "text"), CancellationToken.None));

        payload["TotalCount"]!.Value<int>().Should().Be(3);
        payload["ReturnedCount"]!.Value<int>().Should().Be(3);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
        payload["Note"]!.Type.Should().Be(JTokenType.Null);
    }

    [Test]
    public async Task ExecuteAsync_EmptyResults_SucceedsWithZeroRows()
    {
        SearchReturns(SearchProjectType.Regex);

        var payload = Payload(await CreateSut().ExecuteAsync(Request("x", "regex"), CancellationToken.None));

        payload["TotalCount"]!.Value<int>().Should().Be(0);
        ((JArray)payload["Results"]!).Should().BeEmpty();
        payload["Note"]!.Type.Should().Be(JTokenType.Null);
    }

    [Test]
    public async Task ExecuteAsync_ForwardsParsedSearchTypeToService()
    {
        SearchReturns(SearchProjectType.Regex, File());

        await CreateSut().ExecuteAsync(Request("foo", "regex"), CancellationToken.None);

        _search.Received(1).Search("foo", SearchProjectType.Regex);
        _search.DidNotReceive().Search(Arg.Any<string>(), SearchProjectType.Text);
    }

    // --------------------------------------------------- skip / take / truncation

    [Test]
    public async Task ExecuteAsync_AppliesSkip()
    {
        SearchReturns(SearchProjectType.Text, Files(30));

        var payload = Payload(await CreateSut().ExecuteAsync(Request("x", "text", skip: 10, take: 20), CancellationToken.None));

        payload["ReturnedCount"]!.Value<int>().Should().Be(20);
        Results_First(payload)["Name"]!.Value<string>().Should().Be("f11.cs");
    }

    private static JObject Results_First(JObject payload) => (JObject)((JArray)payload["Results"]!)[0];

    [Test]
    public async Task ExecuteAsync_NonPositiveTake_FallsBackToDefault()
    {
        SearchReturns(SearchProjectType.Text, Files(25));

        var payload = Payload(await CreateSut().ExecuteAsync(Request("x", "text", take: 0), CancellationToken.None));

        payload["ReturnedCount"]!.Value<int>().Should().Be(20);
    }

    [Test]
    public async Task ExecuteAsync_IsTruncated_WhenResultsRemainAfterPage()
    {
        SearchReturns(SearchProjectType.Text, Files(25));

        Payload(await CreateSut().ExecuteAsync(Request("x", "text", skip: 0, take: 20), CancellationToken.None))
            ["IsTruncated"]!.Value<bool>().Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_NotTruncated_OnLastPage()
    {
        SearchReturns(SearchProjectType.Text, Files(25));

        Payload(await CreateSut().ExecuteAsync(Request("x", "text", skip: 20, take: 20), CancellationToken.None))
            ["IsTruncated"]!.Value<bool>().Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_SkipPastEnd_ReturnsNoteAndNotTruncated()
    {
        SearchReturns(SearchProjectType.Text, Files(3));

        var payload = Payload(await CreateSut().ExecuteAsync(Request("x", "text", skip: 20), CancellationToken.None));

        payload["ReturnedCount"]!.Value<int>().Should().Be(0);
        payload["IsTruncated"]!.Value<bool>().Should().BeFalse();
        payload["Note"]!.Value<string>().Should().Contain("past the last result").And.Contain("only 3");
    }

    // ------------------------------------------- pattern -> text fallback

    [Test]
    public async Task ExecuteAsync_PatternWithNoGlob_FallsBackToTextWhenPatternEmpty()
    {
        _search.Search("message-actions", SearchProjectType.Pattern).Returns(new List<WorkspaceFile>());
        _search.Search("message-actions", SearchProjectType.Text).Returns(Files(2).ToList());

        var payload = Payload(await CreateSut()
            .ExecuteAsync(Request("message-actions", "pattern"), CancellationToken.None));

        payload["Type"]!.Value<string>().Should().Be("text");
        payload["TotalCount"]!.Value<int>().Should().Be(2);
        payload["Note"]!.Value<string>().Should().Contain("no wildcard characters");

        _search.Received(1).Search("message-actions", SearchProjectType.Pattern);
        _search.Received(1).Search("message-actions", SearchProjectType.Text);
    }

    [Test]
    public async Task ExecuteAsync_PatternWithGlob_DoesNotFallBack()
    {
        _search.Search("*.cs", SearchProjectType.Pattern).Returns(new List<WorkspaceFile>());

        var payload = Payload(await CreateSut().ExecuteAsync(Request("*.cs", "pattern"), CancellationToken.None));

        payload["Type"]!.Value<string>().Should().Be("pattern");
        payload["TotalCount"]!.Value<int>().Should().Be(0);
        payload["Note"]!.Type.Should().Be(JTokenType.Null);

        _search.DidNotReceive().Search(Arg.Any<string>(), SearchProjectType.Text);
    }

    [Test]
    public async Task ExecuteAsync_PatternNoGlob_WhenTextAlsoEmpty_KeepsPatternResult()
    {
        _search.Search("nothing", SearchProjectType.Pattern).Returns(new List<WorkspaceFile>());
        _search.Search("nothing", SearchProjectType.Text).Returns(new List<WorkspaceFile>());

        var payload = Payload(await CreateSut().ExecuteAsync(Request("nothing", "pattern"), CancellationToken.None));

        payload["Type"]!.Value<string>().Should().Be("pattern");
        payload["TotalCount"]!.Value<int>().Should().Be(0);
        payload["Note"]!.Type.Should().Be(JTokenType.Null);
    }

    [Test]
    public async Task ExecuteAsync_PatternNoGlob_WithMatches_DoesNotFallBack()
    {
        _search.Search("dir", SearchProjectType.Pattern).Returns(Files(1).ToList());

        var payload = Payload(await CreateSut().ExecuteAsync(Request("dir", "pattern"), CancellationToken.None));

        payload["Type"]!.Value<string>().Should().Be("pattern");
        payload["TotalCount"]!.Value<int>().Should().Be(1);

        _search.DidNotReceive().Search(Arg.Any<string>(), SearchProjectType.Text);
    }

    [Test]
    public async Task ExecuteAsync_FallbackAndSkipPastEnd_CombineBothNotes()
    {
        _search.Search("q", SearchProjectType.Pattern).Returns(new List<WorkspaceFile>());
        _search.Search("q", SearchProjectType.Text).Returns(Files(2).ToList());

        var note = Payload(await CreateSut()
                .ExecuteAsync(Request("q", "pattern", skip: 20), CancellationToken.None))
            ["Note"]!.Value<string>();

        note.Should().Contain("no wildcard characters");
        note.Should().Contain("Also:").And.Contain("skip (20)");
    }

    // ----------------------------------------------------- preview budget

    [Test]
    public async Task ExecuteAsync_CapsCumulativePreviewAcrossRows()
    {
        var big = new string('p', 5_000);
        SearchReturns(SearchProjectType.Text,
            File(name: "a", preview: big),
            File(name: "b", preview: big),
            File(name: "c", preview: big));

        var rows = Results(await CreateSut().ExecuteAsync(Request("x", "text"), CancellationToken.None));

        var totalPreview = rows.Sum(r => r["Preview"]!.Value<string>().Length);
        totalPreview.Should().BeLessThanOrEqualTo(8_001); // 8000 budget + one ellipsis char

        rows[2]["Preview"]!.Value<string>().Should().BeEmpty();
    }
}
