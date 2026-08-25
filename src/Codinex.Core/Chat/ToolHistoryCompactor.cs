using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Models;
using Newtonsoft.Json;

namespace Codinex.Core.Chat
{
    [AutoDiRegister(Modules.Chat, RegistrationOrder.Platform)]
    public sealed class ToolHistoryCompactor : IToolHistoryCompactor
    {
        private const int RecentToolResultWindow = 8;

        /// <summary>
        /// Tool results shorter than this are never stubbed. Hiding a small result
        /// (e.g. a single member from read_element) saves only a handful of tokens
        /// but costs a full extra tool round-trip the moment the model needs it again -
        /// a bad trade for tasks that revisit many small members repeatedly.
        /// </summary>
        private const int MinStubbableContentLength = 1500;

        private static readonly string[] IdentifyingArgumentNames =
        {
            "path", "filePath", "query", "symbol", "id", "title", "elementId"
        };

        public IReadOnlyList<ChatMessage> Compact(IReadOnlyList<ChatMessage> history)
        {
            if (history == null || history.Count == 0)
            {
                return history;
            }

            var toolCallLookup = BuildToolCallLookup(history);
            var toolMessageIndices = GetToolMessageIndices(history);

            if (toolMessageIndices.Count == 0)
            {
                return history;
            }

            var supersededIndices = FindSupersededIndices(history, toolMessageIndices, toolCallLookup);
            var stubIndices = FindWindowStubIndices(history, toolMessageIndices, supersededIndices);

            if (stubIndices.Count == 0)
            {
                return history;
            }

            var result = new List<ChatMessage>(history.Count);

            for (var i = 0; i < history.Count; i++)
            {
                if (!stubIndices.Contains(i))
                {
                    result.Add(history[i]);
                    continue;
                }

                var original = history[i];

                toolCallLookup.TryGetValue(original.ToolCallId ?? string.Empty, out var call);

                result.Add(new ChatMessage
                {
                    Role = original.Role,
                    ToolCallId = original.ToolCallId,
                    Content = BuildStubContent(call.Name, call.Arguments, original.Content, supersededIndices.Contains(i)),
                    Data = original.Data,
                    Context = original.Context,
                    CreatedAt = original.CreatedAt
                });
            }

            return result;
        }

        /// <summary>
        /// Marks every tool-result message whose target (tool name + identifying argument)
        /// is repeated by a later call. Only the most recent result per target is kept full,
        /// since a later identical call has already made the earlier one stale.
        /// </summary>
        private static HashSet<int> FindSupersededIndices(
            IReadOnlyList<ChatMessage> history,
            List<int> toolMessageIndices,
            Dictionary<string, (string Name, JObject Arguments)> toolCallLookup)
        {
            var keysByIndex = new Dictionary<int, string>();
            var lastIndexByKey = new Dictionary<string, int>();

            foreach (var index in toolMessageIndices)
            {
                if (ShouldAlwaysKeep(history[index].Content))
                {
                    continue;
                }

                var toolCallId = history[index].ToolCallId ?? string.Empty;
                var key = toolCallLookup.TryGetValue(toolCallId, out var call)
                    ? BuildTargetKey(call.Name, call.Arguments)
                    : null;

                keysByIndex[index] = key;

                if (!string.IsNullOrEmpty(key))
                {
                    lastIndexByKey[key] = index;
                }
            }

            var superseded = new HashSet<int>();

            foreach (var index in toolMessageIndices)
            {
                if (!keysByIndex.TryGetValue(index, out var key))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(key) && lastIndexByKey[key] != index)
                {
                    superseded.Add(index);
                }
            }

            return superseded;
        }

        /// <summary>
        /// Keeps the most recent <see cref="RecentToolResultWindow"/> non-superseded, non-exempt
        /// tool results in full and marks everything older for stubbing. Failed results and results
        /// too small to be worth hiding are always kept in full.
        /// </summary>
        private static HashSet<int> FindWindowStubIndices(
            IReadOnlyList<ChatMessage> history,
            List<int> toolMessageIndices,
            HashSet<int> supersededIndices)
        {
            var stubIndices = new HashSet<int>(supersededIndices);
            var keptFullCount = 0;

            for (var i = toolMessageIndices.Count - 1; i >= 0; i--)
            {
                var index = toolMessageIndices[i];

                if (supersededIndices.Contains(index))
                {
                    continue;
                }

                if (ShouldAlwaysKeep(history[index].Content))
                {
                    continue;
                }

                if (keptFullCount < RecentToolResultWindow)
                {
                    keptFullCount++;
                    continue;
                }

                stubIndices.Add(index);
            }

            return stubIndices;
        }

        private static Dictionary<string, (string Name, JObject Arguments)> BuildToolCallLookup(IReadOnlyList<ChatMessage> history)
        {
            var lookup = new Dictionary<string, (string Name, JObject Arguments)>();

            foreach (var message in history)
            {
                if (message.ToolCalls == null)
                {
                    continue;
                }

                foreach (var call in message.ToolCalls)
                {
                    if (string.IsNullOrWhiteSpace(call.Id))
                    {
                        continue;
                    }

                    lookup[call.Id] = (call.Name, call.Arguments);
                }
            }

            return lookup;
        }

        private static List<int> GetToolMessageIndices(IReadOnlyList<ChatMessage> history)
        {
            var indices = new List<int>();

            for (var i = 0; i < history.Count; i++)
            {
                if (string.Equals(history[i].Role, "tool", StringComparison.OrdinalIgnoreCase))
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        private static string BuildTargetKey(string toolName, JObject arguments)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return null;
            }

            var identifier = GetIdentifyingArgument(arguments);

            return string.IsNullOrEmpty(identifier)
                ? null
                : $"{toolName.ToLowerInvariant()}:{identifier}";
        }

        private static string GetIdentifyingArgument(JObject arguments)
        {
            if (arguments == null)
            {
                return null;
            }

            foreach (var name in IdentifyingArgumentNames)
            {
                var value = arguments.Value<string>(name);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static bool ShouldAlwaysKeep(string content)
        {
            return string.IsNullOrEmpty(content)
                || content.Length < MinStubbableContentLength
                || IsFailedResult(content);
        }

        private static bool IsFailedResult(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            try
            {
                var obj = JObject.Parse(content);

                return obj["success"]?.Value<bool>() == false;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        private static string BuildStubContent(string toolName, JObject arguments, string originalContent, bool superseded)
        {
            var reason = superseded
                ? "superseded by a later call with the same target"
                : "outside the recent tool-result window";
            var identifier = GetIdentifyingArgument(arguments);
            var descriptor = string.IsNullOrEmpty(identifier) ? string.Empty : $" ({identifier})";
            var length = originalContent?.Length ?? 0;

            return $"[tool result hidden to save context: {toolName ?? "tool"}{descriptor}, {reason}, {length} chars. Call the tool again if this data is still needed.]";
        }
    }
}
