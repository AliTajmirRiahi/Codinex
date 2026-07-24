using System;
using System.Collections.Generic;
using Codify.Core.Models;
using Codify.Tests.VisualStudio.Workspace.FormatterTests.OpenDocumentsFormatterTest.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.FormatterTests.OpenDocumentsFormatterTest;

[TestFixture]
public sealed class OpenDocumentsFormatter_FormatTests
    : OpenDocumentsFormatterTestBase
{
    [Test]
    public void Format_Should_ThrowArgumentNullException_WhenDocumentsIsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Action action = () => sut.Format(null);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Format_Should_ReturnEmptyString_WhenDocumentsIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Format([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void Format_Should_FormatSingleDocument()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<ReferenceItem> documents =
        [
            new ReferenceItem
            {
                Name = "Program.cs"
            }
        ];

        // Act
        var result = sut.Format(documents);

        // Assert
        result.Should().Contain("Program.cs");
    }

    [Test]
    public void Format_Should_FormatMultipleDocuments()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<ReferenceItem> documents =
        [
            new ReferenceItem
            {
                Name = "Program.cs"
            },
            new ReferenceItem
            {
                Name = "OrderService.cs"
            },
            new ReferenceItem
            {
                Name = "UserRepository.cs"
            }
        ];

        // Act
        var result = sut.Format(documents);

        // Assert
        result.Should().Contain("Program.cs");
        result.Should().Contain("OrderService.cs");
        result.Should().Contain("UserRepository.cs");
    }
}