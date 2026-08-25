using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codinex.Core.Models.AI
{
    public static class AiPreprocessorResultParser
    {
        public static AiPreprocessorResult ParseOrDefault(string response, string user = null)
        {
            return TryParse(response, out var result)
                ? result
                : AiPreprocessorResult.CreateForwardFallback(user);
        }

        public static bool TryParse(
            string response,
            out AiPreprocessorResult result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(response))
            {
                return false;
            }

            try
            {
                var json = ExtractJsonObject(response.Trim());
                var obj = JObject.Parse(json);
                var action = obj.Value<string>("action");

                if (!string.Equals(action, "answer", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(action, "forward", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                result = obj.ToObject<AiPreprocessorResult>();

                if (result == null)
                {
                    return false;
                }

                result.ContextsNeeded = result.ContextsNeeded ?? new List<string>();
                result.Intents = result.Intents ?? new List<string>();

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string ExtractJsonObject(string response)
        {
            if (response.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = response.IndexOf('\n');
                var fenceEnd = response.LastIndexOf("```", StringComparison.Ordinal);

                if (firstLineEnd >= 0 && fenceEnd > firstLineEnd)
                {
                    response = response.Substring(firstLineEnd + 1, fenceEnd - firstLineEnd - 1).Trim();
                }
            }

            if (response.StartsWith("{", StringComparison.Ordinal) &&
                response.EndsWith("}", StringComparison.Ordinal))
            {
                return response;
            }

            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');

            return start >= 0 && end > start
                ? response.Substring(start, end - start + 1)
                : response;
        }
    }
}
