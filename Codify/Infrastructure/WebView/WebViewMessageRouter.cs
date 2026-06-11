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
    private readonly IPayloadBinder _payloadBinder;

    public WebViewMessageRouter(
        ISendChatMessageUseCase sendChatMessageUseCase,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        ProviderManager providerManager,
        IPayloadBinder payloadBinder)
    {
        _sendChatMessageUseCase = sendChatMessageUseCase;
        _webViewClient = webViewClient;
        _serializer = serializer;
        _providerManager = providerManager;
        _payloadBinder = payloadBinder;
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

        WebViewMessage request;

        try
        {
            request = _serializer.Deserialize<WebViewMessage>(messageJson);
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

        switch (request.Type)
        {
            case WebViewMessageType.Ready:
                {
                    // UI initialized and ready
                    await SendInitialDataAsync();
                    return;
                }

            case WebViewMessageType.InitState:
                {
                    // UI asks for current state
                    await SendInitialDataAsync();
                    return;
                }

            case WebViewMessageType.SendMessage:
                {
                    var payload = _payloadBinder.Bind<ChatMessage>(request.Payload);

                    var response = await _sendChatMessageUseCase.ExecuteAsync(payload, false);

                    await _webViewClient.PostMessageAsync(response);
                    return;
                }

            case WebViewMessageType.SelectProvider:
                {
                    //var payload = _payloadBinder.Bind<SelectProviderPayload>(request.Payload);

                    //_providerManager.SetActiveProvider(payload.ProviderId);

                    //await SendInitialDataAsync();
                    return;
                }

            case WebViewMessageType.CancelGeneration:
                {
                    // Future: cancel streaming AI response
                    // Example: _generationCancellation.Cancel();

                    return;
                }

            default:
                {
                    await _webViewClient.PostMessageAsync(new ChatResponse(
                        WebViewMessageType.Error,
                        $"Unknown message type: {request.Type}"));
                    return;
                }


        }
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
                Providers = new
                {
                    availableProviders = providers,
                    current = _providerManager.ActiveProvider
                },
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }
}