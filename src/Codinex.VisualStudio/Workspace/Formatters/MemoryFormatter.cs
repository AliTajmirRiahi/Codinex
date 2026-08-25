using System;
using System.Text;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.Workspace.Formatters
{
    /// <summary>
    /// Formats workspace memory into prompt text.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
    public sealed class MemoryFormatter : IMemoryContextFormatter
    {
        public string Format(MemoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.MemoryDocument == null)
                throw new ArgumentNullException(nameof(MemoryDocument));

            if (context.MemoryDocument.Facts.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();

            foreach (var fact in context.MemoryDocument.Facts)
            {
                builder.AppendLine($"Id: {fact.Id}");
                builder.AppendLine($"Title: {fact.Title}");
                builder.AppendLine($"Content: {fact.Content}");
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
    }
}