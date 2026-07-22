using System.Collections.Generic;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.Infrastructure.Workspace.PromptPipeline.PromptContextComposerTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.PromptContextComposerTests;

[TestFixture]
public class PromptContextComposer_ComposeTests
    : PromptContextComposerTestBase
{
    [Test]
    public void Compose_Should_ReturnEmptyPromptContext_WhenProviderResultsAreEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Compose(new List<ContextProviderResult>());

        // Assert
        result.Should().NotBeNull();
        result.Sections.Should().BeEmpty();
    }

    [Test]
    public void Compose_Should_IgnoreEmptyProviderResults()
    {
        // Arrange
        var sut = CreateSut();

        var empty = new ContextProviderResult();

        var notEmpty = new ContextProviderResult();

        notEmpty.Items.Add(new PromptContextItem
        {
            Title = "Item"
        });

        // Act
        var result = sut.Compose([
            empty,
            notEmpty
        ]);

        // Assert
        result.Sections.Should().HaveCount(1);

        result.Sections[0].Items.Should().HaveCount(1);

        result.Sections[0].Items[0].Title.Should().Be("Item");
    }

    [Test]
    public void Compose_Should_CreateWorkspaceSection_WhenItemsExist()
    {
        // Arrange
        var sut = CreateSut();

        var providerResult = new ContextProviderResult();

        providerResult.Items.Add(new PromptContextItem());

        // Act
        var result = sut.Compose([
            providerResult
        ]);

        // Assert
        result.Sections.Should().HaveCount(1);

        result.Sections[0].Name.Should().Be("Workspace");
    }

    [Test]
    public void Compose_Should_CopyAllItemsIntoWorkspaceSection()
    {
        // Arrange
        var sut = CreateSut();

        var first = new ContextProviderResult();

        first.Items.Add(new PromptContextItem
        {
            Title = "Item1"
        });

        first.Items.Add(new PromptContextItem
        {
            Title = "Item2"
        });

        var second = new ContextProviderResult();

        second.Items.Add(new PromptContextItem
        {
            Title = "Item3"
        });

        // Act
        var result = sut.Compose([
            first,
            second
        ]);

        // Assert
        result.Sections.Should().HaveCount(1);

        result.Sections[0].Items.Should().HaveCount(3);

        result.Sections[0].Items[0].Title.Should().Be("Item1");
        result.Sections[0].Items[1].Title.Should().Be("Item2");
        result.Sections[0].Items[2].Title.Should().Be("Item3");
    }

    [Test]
    public void Compose_Should_PreserveItemsOrder()
    {
        // Arrange
        var sut = CreateSut();

        var providerResult = new ContextProviderResult();

        providerResult.Items.Add(new PromptContextItem
        {
            Title = "First"
        });

        providerResult.Items.Add(new PromptContextItem
        {
            Title = "Second"
        });

        providerResult.Items.Add(new PromptContextItem
        {
            Title = "Third"
        });

        // Act
        var result = sut.Compose([
            providerResult
        ]);

        // Assert
        result.Sections[0].Items[0].Title.Should().Be("First");
        result.Sections[0].Items[1].Title.Should().Be("Second");
        result.Sections[0].Items[2].Title.Should().Be("Third");
    }

    [Test]
    public void Compose_Should_UseSamePromptContextItemInstances()
    {
        // Arrange
        var sut = CreateSut();

        var item = new PromptContextItem
        {
            Title = "Item"
        };

        var providerResult = new ContextProviderResult();

        providerResult.Items.Add(item);

        // Act
        var result = sut.Compose([
            providerResult
        ]);

        // Assert
        result.Sections.Should().HaveCount(1);

        result.Sections[0].Items.Should().ContainSingle();

        result.Sections[0].Items[0]
            .Should()
            .BeSameAs(item);
    }
}