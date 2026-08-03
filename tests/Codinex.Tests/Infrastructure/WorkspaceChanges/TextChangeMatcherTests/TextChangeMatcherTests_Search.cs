using System.IO;
using System.Linq;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_Search : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldReturnSearchNotFound_WhenSearchDoesNotExist()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "XYZ"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
        result.MatchCount.Should().Be(0);
        result.Error.Should().Be("Search text was not found.");
    }

    [Test]
    public void Match_ShouldReturnSuccess_WhenSearchExistsOnce()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchCount.Should().Be(1);
        result.StartIndex.Should().Be(6);
        result.Length.Should().Be(5);
        result.MatchedText.Should().Be("World");
        result.Error.Should().BeNull();
    }

    [Test]
    public void Match_ShouldReturnMultipleMatches_WhenSearchExistsTwice()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "abc"
        };

        // Act
        var result = sut.Match("abc 123 abc", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
        result.Error.Should().Be("Multiple matching locations were found.");
    }

    [Test]
    public void Match_ShouldReturnMultipleMatches_WhenSearchExistsThreeTimes()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "abc"
        };

        // Act
        var result = sut.Match("abc abc abc", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(3);
    }

    [Test]
    public void Match_ShouldFindSearchAtBeginningOfContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
    }

    [Test]
    public void Match_ShouldFindSearchAtEndOfContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(6);
    }

    [Test]
    public void Match_ShouldMatchWholeContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
        result.Length.Should().Be(11);
    }

    [Test]
    public void Match_ShouldReturnSearchNotFound_WhenContentIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello"
        };

        // Act
        var result = sut.Match(string.Empty, change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
        result.MatchCount.Should().Be(0);
    }

    //[Test]
    //public void Match_Should_Find_Search_Text()
    //{
    //    var sut = CreateSut();

    //    var content = File.ReadAllText(@"P:\03 ARTA PROJECTS\01 - Self Update\PixelForge\PixelForge\src\PixelForge.Api\Program.cs");

    //    var change = Newtonsoft.Json.JsonConvert.DeserializeObject<EditFileChange>(
    //        "{\"FilePath\":\"src\\\\PixelForge.Api\\\\Program.cs\",\"TextChanges\":[{\"Id\":\"00000000-0000-0000-0000-000000000000\",\"Order\":1,\"Before\":\"\",\"Search\":\"        options.SwaggerEndpoint(\\\"/swagger/v1/swagger.json\\\", \\\"PixelForge V1 Docs\\\"\\n        //options.SwaggerEndpoint(\\\"/swagger/v2/swagger.json\\\", \\\"V2 Docs\\\");\",\"Replace\":\"        options.SwaggerEndpoint(\\\"/swagger/v1/swagger.json\\\", \\\"PixelForge V1 Docs\\\");\\n        //options.SwaggerEndpoint(\\\"/swagger/v2/swagger.json\\\", \\\"V2 Docs\\\");\",\"After\":\"\"}],\"Kind\":0,\"Id\":\"43ad6af3-de69-48ed-806f-6578b8194422\"}");

    //    var result = sut.Match(content, change.TextChanges.ElementAt(0));

    //    result.Status.Should().Be(TextChangeMatchStatus.Success);
    //    result.MatchCount.Should().Be(1);
    //}
}