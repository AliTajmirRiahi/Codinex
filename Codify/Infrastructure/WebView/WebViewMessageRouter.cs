using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Core.UseCases;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Storage;
using Codify.Storage.Models.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using Codify.Infrastructure.Factory;

namespace Codify.Infrastructure.WebView;

/// <summary>
/// Routes messages from WebView UI to application use cases.
/// </summary>
public sealed class WebViewMessageRouter : IWebViewMessageRouter
{
    private readonly IWebViewClient _webViewClient;
    private readonly IJsonSerializer _serializer;
    private readonly ProviderManager _providerManager;
    private readonly IPayloadBinder _payloadBinder;
    private readonly ChatUseCaseFactory _chatUseCaseFactory;
    private readonly ChatSessionService _sessionService;

    private ISendChatMessageUseCase _sendChatMessageUseCase;

    public WebViewMessageRouter(
        ProviderManager providerManager,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        IPayloadBinder payloadBinder,
        ChatUseCaseFactory chatUseCaseFactory,
        ChatSessionService sessionService)
    {
        _providerManager = providerManager;
        _webViewClient = webViewClient;
        _serializer = serializer;
        _payloadBinder = payloadBinder;
        _chatUseCaseFactory = chatUseCaseFactory;
        _sessionService = sessionService;
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
                    await _sessionService.InitializeAsync();

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
                    _sendChatMessageUseCase = _chatUseCaseFactory.Create();

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
            case WebViewMessageType.SelectModel:
                {
                    var payload = _payloadBinder.Bind<AiModelSelectedDto>(request.Payload);

                    await _providerManager.SetCurrentModelAsync(payload);

                    //await SendInitialDataAsync();
                    return;
                }

            case WebViewMessageType.CancelGeneration:
                {
                    // Future: cancel streaming AI response
                    // Example: _generationCancellation.Cancel();

                    return;
                }
            case WebViewMessageType.UpdateSettings:
                {
                    // Future: update provider settings
                    var aiProviderDto = _payloadBinder.Bind<AiProviderDto>(request.Payload);
                    await _providerManager.UpdateSettingsAsync(aiProviderDto);
                    await SendSelectedProviderDataAsync();
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
                    AvailableProviders = providers,
                    Current = _providerManager.ActiveProvider
                },
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendSelectedProviderDataAsync()
    {
        // Get all configured providers and their models from ProviderManager
        var provider = _providerManager.AllProviders.FirstOrDefault(p => p.IsEnabled);

        var message = new
        {
            Type = WebViewMessageType.SelectProvider,
            Payload = new
            {
                provider = provider,
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }


}