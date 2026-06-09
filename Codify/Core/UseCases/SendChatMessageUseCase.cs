using Codify.Core.Abstractions;
using Codify.Core.Models;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Core.UseCases;

/// <summary>
/// Orchestrates the flow of sending a message to AI. 
/// Located in Core because it represents the "Business Logic" of the extension.
/// </summary>
public sealed class SendChatMessageUseCase : ISendChatMessageUseCase
{
    private readonly IAiProvider _aiProvider;

    // We depend on the Interface (Abstraction), not the concrete implementation.
    // This makes it easy to swap GapGPT with Local AI or OpenAI.
    public SendChatMessageUseCase(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public async Task<ChatResponse> ExecuteAsync(ChatMessage message, bool includeSelectedCode)
    {
        if (message == null)
        {
            return new ChatResponse("ERROR", "Message cannot be empty.");
        }

        try
        {
            //var attachments = new List<Attachment>();

            //if (includeSelectedCode)
            //{
            //    string code = await _visualStudioService.GetSelectedCodeAsync();
            //    attachments.Add(new Attachment
            //    {
            //        Type = AttachmentType.CodeSnippet,
            //        Content = code
            //    });
            //}

            // Call the provider (this could be GapGPT, Ollama, etc.)
            var aiResult = await _aiProvider.SendAsync(message);

            return new ChatResponse("AI_RESPONSE", aiResult);
        }
        catch (Exception ex)
        {
            return new ChatResponse("ERROR", $"AI Provider Error: {ex.Message}");
        }
    }
}