using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Core.UseCases;
using Codify.Storage;
using System;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WebView;

/// <summary>
/// Routes messages from WebView UI to application use cases.
/// </summary>
public sealed class WebViewMessageRouter : IWebViewMessageRouter
{
    private readonly ISendChatMessageUseCase _sendChatMessageUseCase;
    private readonly IWebViewClient _webViewClient;
    private readonly IJsonSerializer _serializer;
    private readonly ProviderManager _providerManager;

    public WebViewMessageRouter(
        ISendChatMessageUseCase sendChatMessageUseCase,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        ProviderManager providerManager)
    {
        _sendChatMessageUseCase = sendChatMessageUseCase;
        _webViewClient = webViewClient;
        _serializer = serializer;
        _providerManager = providerManager;
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

    public async Task SendInitialDataAsync()
    {
        // Get all configured providers and their models from ProviderManager
        var providers = _providerManager.AllProviders;

        var message = new
        {
            Type = WebViewMessageType.InitData,
            Payload = new
            {
                Providers = providers,
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }
}