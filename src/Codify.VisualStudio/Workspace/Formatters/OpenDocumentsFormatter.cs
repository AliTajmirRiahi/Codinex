using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Codify.VisualStudio.Workspace.Formatters
{
    /// <summary>
    /// Formats open documents into prompt text.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
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