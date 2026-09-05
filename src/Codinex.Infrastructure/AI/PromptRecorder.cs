using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Storage.Services;

namespace Codinex.Infrastructure.AI
{
    /// <summary>
    /// Writes the raw outgoing AI request payload for a chat turn to
    /// %LocalAppData%\Codinex\prompts\chat_&lt;chatId&gt;\&lt;chatMessageId&gt;\prompt_&lt;guid&gt;.json
    /// so it can later be attached to bug reports. Every provider funnels its fully
    /// serialized request through here right before it is sent, which also makes this
    /// the single choke point for the large-prompt size check.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class PromptRecorder(
        IWorkspaceFileService workspaceFileService,
        IPromptSizeGuard promptSizeGuard) : IPromptRecorder
    {
        public async Task RecordAsync(
            string chatId,
            string chatMessageId,
            string payloadContent,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(chatMessageId))
            {
                return;
            }

            // Gate on the size of the exact bytes about to be sent/persisted (the whole
            // payload: messages + tools + envelope). Stop throws OperationCanceledException;
            // AiErrorFactory only maps OCE to an error when the token itself is cancelled
            // (it is not here), so the providers' error wrappers re-throw it and the send
            // unwinds through the normal user-cancellation path.
            var payloadByteCount = Encoding.UTF8.GetByteCount(payloadContent ?? string.Empty);

            if (!await promptSizeGuard.ConfirmAsync(payloadByteCount, chatId, cancellationToken))
            {
                throw new OperationCanceledException(
                    "The user stopped the request because the prompt exceeded the configured size limit.");
            }

            var folder = StoragePaths.GetChatMessagePromptsPath(chatId, chatMessageId);
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
