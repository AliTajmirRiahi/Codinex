using System.Linq;
using System.Text;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.Core.Chat
{
    [AutoDiRegister(Modules.Chat, RegistrationOrder.Platform)]
    public sealed class ReferenceContextFormatter : IReferenceContextFormatter
    {
        public string Format(ReferenceItem reference)
        {
            return reference.Type switch
            {
                ReferenceKind.File => FormatFile(reference),
                ReferenceKind.Method => FormatSymbol(reference, "Method"),
                ReferenceKind.Class => FormatSymbol(reference, "Class"),
                ReferenceKind.Interface => FormatSymbol(reference, "Interface"),
                ReferenceKind.Field => FormatSymbol(reference, "Field"),
                ReferenceKind.Folder => FormatFolder(reference),
                ReferenceKind.Project => FormatProject(reference),
                ReferenceKind.Solution => FormatSolution(reference),
                ReferenceKind.Output => FormatContentBlock(reference, "Output"),
                ReferenceKind.Log => FormatContentBlock(reference, "Log"),
                ReferenceKind.Git => FormatContentBlock(reference, "Git"),
                ReferenceKind.Image => FormatImage(reference),
                _ => FormatGeneric(reference)
            };
        }

        private static string FormatFile(ReferenceItem reference)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[File]");
            sb.AppendLine($"Name: {reference.Name}");
            sb.AppendLine($"Path: {reference.Value}");

            if (!string.IsNullOrWhiteSpace(reference.Description))
            {
                sb.AppendLine($"Description: {reference.Description}");
            }

            AppendElements(sb, reference);

            if (reference.Metadata.Elements?.Any() == true || string.IsNullOrWhiteSpace(reference.Metadata.Content)) return sb.ToString().TrimEnd();

            sb.AppendLine("Content:");
            sb.AppendLine(reference.Metadata.Content);

            return sb.ToString().TrimEnd();
        }

        private static void AppendElements(StringBuilder sb, ReferenceItem reference)
        {
            if (reference.Metadata.Elements?.Any() != true)
            {
                return;
            }

            sb.AppendLine("Elements:");

            foreach (var element in reference.Metadata.Elements)
            {
                sb.AppendLine($"- id: {element.Id}");
                sb.AppendLine($"  kind: {element.Kind}");
                sb.AppendLine($"  name: {element.Name}");

                if (!string.IsNullOrWhiteSpace(element.Signature))
                {
                    sb.AppendLine($"  signature: {element.Signature}");
                }
            }
        }

        private static string FormatSymbol(ReferenceItem reference, string kindName)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"[{kindName}]");
            sb.AppendLine($"Name: {reference.Name}");

            if (!string.IsNullOrWhiteSpace(reference.Value))
            {
                sb.AppendLine($"Path: {reference.Value}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Description))
            {
                sb.AppendLine($"Description: {reference.Description}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Metadata.Signature))
            {
                sb.AppendLine($"Signature: {reference.Metadata.Signature}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Metadata.ContainerName))
            {
                sb.AppendLine($"Container: {reference.Metadata.ContainerName}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Metadata.Content))
            {
                sb.AppendLine("Content:");
                sb.AppendLine(reference.Metadata.Content);
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatFolder(ReferenceItem reference)
        {
            return BuildSimpleBlock("Folder", reference);
        }

        private static string FormatProject(ReferenceItem reference)
        {
            return BuildSimpleBlock("Project", reference);
        }

        private static string FormatSolution(ReferenceItem reference)
        {
            return BuildSimpleBlock("Solution", reference);
        }

        private static string FormatContentBlock(ReferenceItem reference, string kindName)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"[{kindName}]");
            sb.AppendLine($"Name: {reference.Name}");

            if (!string.IsNullOrWhiteSpace(reference.Value))
            {
                sb.AppendLine($"Path: {reference.Value}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Metadata.Content))
            {
                sb.AppendLine("Content:");
                sb.AppendLine(reference.Metadata.Content);
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatImage(ReferenceItem reference)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[Image]");
            sb.AppendLine($"Name: {reference.Name}");

            if (!string.IsNullOrWhiteSpace(reference.Value))
            {
                sb.AppendLine($"Path: {reference.Value}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Metadata.Signature))
            {
                sb.AppendLine($"Mime Type: {reference.Metadata.Signature}");
            }

            sb.AppendLine("Content: Uploaded image attached as image input.");

            return sb.ToString().TrimEnd();
        }

        private static string FormatGeneric(ReferenceItem reference)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[Reference]");
            sb.AppendLine($"Kind: {reference.Type}");
            sb.AppendLine($"Name: {reference.Name}");

            if (!string.IsNullOrWhiteSpace(reference.Value))
            {
                sb.AppendLine($"Value: {reference.Value}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Description))
            {
                sb.AppendLine($"Description: {reference.Description}");
            }

            if (string.IsNullOrWhiteSpace(reference.Metadata.Content)) return sb.ToString().TrimEnd();

            sb.AppendLine("Content:");
            sb.AppendLine(reference.Metadata.Content);

            return sb.ToString().TrimEnd();
        }

        private static string BuildSimpleBlock(string kindName, ReferenceItem reference)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"[{kindName}]");
            sb.AppendLine($"Name: {reference.Name}");

            if (!string.IsNullOrWhiteSpace(reference.Value))
            {
                sb.AppendLine($"Path: {reference.Value}");
            }

            if (!string.IsNullOrWhiteSpace(reference.Description))
            {
                sb.AppendLine($"Description: {reference.Description}");
            }

            if (string.IsNullOrWhiteSpace(reference.Metadata.Content)) return sb.ToString().TrimEnd();

            sb.AppendLine("Content:");
            sb.AppendLine(reference.Metadata.Content);

            return sb.ToString().TrimEnd();
        }
    }
}
