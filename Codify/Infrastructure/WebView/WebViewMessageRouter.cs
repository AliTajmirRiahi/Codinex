using System.Threading.Tasks;
using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Core.UseCases;

namespace Codify.Infrastructure.WebView;

/// <summary>
/// Routes messages from WebView UI to application use cases.
/// </summary>
public sealed class WebViewMessageRouter : IWebViewMessageRouter
{
    private readonly ISendChatMessageUseCase _sendChatMessageUseCase;
    private readonly IWebViewClient _webViewClient;
    private readonly IJsonSerializer _serializer;

    public WebViewMessageRouter(
        ISendChatMessageUseCase sendChatMessageUseCase,
        IWebViewClient webViewClient,
        IJsonSerializer serializer)
    {
        _sendChatMessageUseCase = sendChatMessageUseCase;
        _webViewClient = webViewClient;
        _serializer = serializer;
    }

    public async Task HandleMessageAsync(string messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            await _webViewClient.PostMessageAsync(new ChatResponse(
                WebViewMessageType.Error,
                "Empty message received."));
            return;
        }

        ChatRequest? request;
        try
        {
            request = _serializer.Deserialize<ChatRequest>(messageJson);
        }
        catch
        {
            await _webViewClient.PostMessageAsync(new ChatResponse(
                WebViewMessageType.Error,
                "Invalid message format."));
            return;
        }

        if (request is null)
        {
            await _webViewClient.PostMessageAsync(new ChatResponse(
                WebViewMessageType.Error,
                "Message could not be parsed."));
            return;
        }

        var response = await _sendChatMessageUseCase.ExecuteAsync(request, false);
        await _webViewClient.PostMessageAsync(response);
    }
}