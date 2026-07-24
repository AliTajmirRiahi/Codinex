using System;
using System.Collections.Generic;
using System.Text;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.VisualStudio.Workspace.Formatters
{
    /// <summary>
    /// Formats open documents into prompt text.
    /// </summary>
    public sealed class OpenDocumentsFormatter
        : IOpenDocumentsFormatter
    {
        public string Format(
            IReadOnlyList<ReferenceItem> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            if (documents.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var document in documents)
            {
                builder.AppendLine(document.Name);
            }

            return builder.ToString().TrimEnd();
        }
    }
}