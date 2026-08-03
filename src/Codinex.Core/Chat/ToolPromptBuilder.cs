using Codify.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codify.Core.Chat
{
    internal static class ToolPromptBuilder
    {
        public static string Build(IEnumerable<IAiTool> tools)
        {
            var toolList = tools.ToList();

            if (toolList.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine("You have access to the following IDE tools:");
            builder.AppendLine();

            foreach (var tool in toolList)
            {
                builder.Append("- ");
                builder.Append(tool.Name);

                if (!string.IsNullOrWhiteSpace(tool.Description))
                {
                    builder.Append(": ");
                    builder.Append(tool.Description);
                }

                builder.AppendLine();
            }

            builder.AppendLine();
            builder.AppendLine("Use these tools whenever additional information from the workspace is required.");
            builder.AppendLine("Do not guess codebase details when a tool can retrieve them.");

            return builder.ToString();
        }
    }
}
