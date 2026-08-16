using System;
using System.Collections.Concurrent;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;

namespace Codinex.Infrastructure.AI.Providers.OpenCode
{
    /// <summary>
    /// In-memory mapping between a Codinex chat session id and the OpenCode server session id
    /// created for it. Lives for the lifetime of the extension, which is sufficient to reuse the
    /// OpenCode session across every turn of an open conversation.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class OpenCodeSessionStore : IOpenCodeSessionStore
    {
        private readonly ConcurrentDictionary<string, string> _sessions = new(StringComparer.Ordinal);

        public bool TryGetSessionId(string conversationId, out string openCodeSessionId)
        {
            openCodeSessionId = null;

            return !string.IsNullOrWhiteSpace(conversationId)
                   && _sessions.TryGetValue(conversationId, out openCodeSessionId);
        }

        public void SetSessionId(string conversationId, string openCodeSessionId)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(openCodeSessionId))
                return;

            _sessions[conversationId] = openCodeSessionId;
        }
    }
}
