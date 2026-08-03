using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Codify.VisualStudio.Workspace.Formatters
{
    /// <summary>
    /// Formats diagnostics into prompt text.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
    public sealed class DiagnosticsFormatter : IDiagnosticsFormatter
    {
        public string Format(
            IReadOnlyList<DiagnosticItem> diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (diagnostics.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var diagnostic in diagnostics)
            {
                builder.AppendLine(
                    $"{diagnostic.Severity} {diagnostic.Id}: {diagnostic.Message}");

                builder.AppendLine(
                    $"File: {Path.GetFileName(diagnostic.FilePath)}({diagnostic.Line},{diagnostic.Column})");

                builder.AppendLine(
                    $"Project: {diagnostic.ProjectName}");

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
    }
}