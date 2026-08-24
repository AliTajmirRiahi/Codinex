using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Storage.Services;

namespace Codinex.Infrastructure.AI
{
    /// <summary>
    /// Writes the raw outgoing AI request payload for a chat turn to
    /// %LocalAppData%\Codinex\prompts\chat_&lt;chatId&gt;\&lt;chatMessageId&gt;\prompt_&lt;guid&gt;.json
    /// so it can later be attached to bug reports.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class PromptRecorder(IWorkspaceFileService workspaceFileService) : IPromptRecorder
    {
        public async Task RecordAsync(
            string chatId,
            string chatMessageId,
            string payloadContent,
            CancellationToken cancellationToken = default)
        {
            var safeChatId = string.IsNullOrWhiteSpace(chatId) ? "unknown" : chatId;
            var safeChatMessageId = string.IsNullOrWhiteSpace(chatMessageId) ? "unknown" : chatMessageId;

            var folder = StoragePaths.GetChatMessagePromptsPath(safeChatId, safeChatMessageId);
            var path = Path.Combine(folder, $"prompt_{Guid.NewGuid()}.json");

            if (!workspaceFileService.DirectoryExists(folder))
            {
                await workspaceFileService.CreateDirectoryAsync(folder, cancellationToken);
            }

            await workspaceFileService.CreateFileAsync(path, cancellationToken);

            await workspaceFileService.WriteAsync(path, payloadContent, cancellationToken: cancellationToken);
        }
    }
}
