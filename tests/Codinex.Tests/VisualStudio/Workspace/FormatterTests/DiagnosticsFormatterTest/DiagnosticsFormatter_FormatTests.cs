using System;
using System.Collections.Generic;
using Codify.Core.Models;
using Codify.Tests.VisualStudio.Workspace.FormatterTests.DiagnosticsFormatterTest.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.FormatterTests.DiagnosticsFormatterTest;

[TestFixture]
public sealed class DiagnosticsFormatter_FormatTests : DiagnosticsFormatterTestBase
{
    [Test]
    public void Format_Should_ThrowArgumentNullException_WhenDiagnosticsIsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Action action = () => sut.Format(null);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Format_Should_ReturnEmptyString_WhenDiagnosticsIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Format([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void Format_Should_FormatSingleDiagnostic()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<DiagnosticItem> diagnostics =
        [
            new DiagnosticItem
            {
                Severity = DiagnosticSeverity.Error,
                Id = "CS1002",
                Message = "; expected",
                FilePath = @"C:\Project\Program.cs",
                ProjectName = "Codify",
                Line = 15,
                Column = 8
            }
        ];

        // Act
        var result = sut.Format(diagnostics);

        // Assert
        result.Should().Contain("Error CS1002: ; expected");
        result.Should().Contain("Program.cs");
        result.Should().Contain("15");
        result.Should().Contain("8");
        result.Should().Contain("Codify");
    }

    [Test]
    public void Format_Should_FormatMultipleDiagnostics()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<DiagnosticItem> diagnostics =
        [
            new DiagnosticItem
            {
                Severity = DiagnosticSeverity.Error,
                Id = "CS1002",
                Message = "; expected",
                FilePath = @"C:\Project\Program.cs",
                ProjectName = "Codify",
                Line = 15,
                Column = 8
            },
            new DiagnosticItem
            {
                Severity = DiagnosticSeverity.Warning,
                Id = "CS8618",
                Message = "Non-nullable property must contain a value.",
                FilePath = @"C:\Project\User.cs",
                ProjectName = "Codify",
                Line = 42,
                Column = 13
            }
        ];

        // Act
        var result = sut.Format(diagnostics);

        // Assert
        result.Should().Contain("CS1002");
        result.Should().Contain("CS8618");
        result.Should().Contain("Program.cs");
        result.Should().Contain("User.cs");
    }
}