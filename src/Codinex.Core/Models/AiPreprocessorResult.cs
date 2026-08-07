using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codinex.Core.Models
{
    public sealed class AiPreprocessorResult
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("needsPlanner")]
        public bool NeedsPlanner { get; set; }

        [JsonProperty("needsWorkspaceContext")]
        public bool NeedsWorkspaceContext { get; set; }

        [JsonProperty("contextsNeeded")]
        public List<string> ContextsNeeded { get; set; } = [];

        [JsonProperty("intents")]
        public List<string> Intents { get; set; } = [];


        public bool IsAnswer =>
            string.Equals(Action, "answer", StringComparison.OrdinalIgnoreCase);

        public bool IsForward =>
            string.Equals(Action, "forward", StringComparison.OrdinalIgnoreCase);

        public static AiPreprocessorResult CreateForwardFallback(string user = null)
        {
            return new AiPreprocessorResult
            {
                Action = "forward",
                User = user,
                NeedsPlanner = false,
                NeedsWorkspaceContext = false,
                ContextsNeeded = new List<string>(),
                Intents = new List<string>()
            };
        }
    }
}
