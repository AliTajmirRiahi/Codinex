using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Core.UseCases;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Infrastructure.Errors;
using Codify.Infrastructure.Factory;
using Codify.Storage;
using Microsoft.VisualStudio;
using System;
using System.Linq;
using System.Threading.Tasks;
using Codify.Infrastructure.References;
using Codify.Storage.Models;
using Codify.Storage.Models.DTO;

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
    private readonly ChatManager _chatManager;
    private readonly IErrorHandler _errorHandler;
    private readonly ReferenceManager _referenceManager;

    private ISendChatMessageUseCase _sendChatMessageUseCase;

    public WebViewMessageRouter(
        ProviderManager providerManager,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        IPayloadBinder payloadBinder,
        ChatUseCaseFactory chatUseCaseFactory,
        ChatSessionService sessionService,
        ChatManager chatManager,
        IErrorHandler errorHandler,
        ReferenceManager referenceManager)
    {
        _providerManager = providerManager;
        _webViewClient = webViewClient;
        _serializer = serializer;
        _payloadBinder = payloadBinder;
        _chatUseCaseFactory = chatUseCaseFactory;
        _sessionService = sessionService;
        _chatManager = chatManager;
        _errorHandler = errorHandler;
        _referenceManager = referenceManager;
    }

    public async Task HandleMessageAsync(string messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
            throw new InvalidOperationException("Empty message received.");

        var request = _serializer.Deserialize<WebViewMessageRequest>(messageJson);

        if (request is null)
            throw new InvalidOperationException("Message could not be parsed.");

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
                    await AskAiAssistantAsync(request);

                    return;
                }

            case WebViewMessageType.CancelGeneration:
                {
                    // Future: cancel streaming AI response
                    // Example: _generationCancellation.Cancel();

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
            case WebViewMessageType.SelectChat:
                {
                    var payload = _payloadBinder.Bind<ChatSelectedDto>(request.Payload);

                    await _sessionService.LoadSessionAsync(payload.ChatId);

                    await SendSelectedChatApprovedAsync();

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
            case WebViewMessageType.NewChat:
                {
                    await EnsureActiveChatSessionAsync();
                    return;
                }
            case WebViewMessageType.DeleteChat:
                {
                    await DeleteChatSessionAsync();
                    return;
                }
            case WebViewMessageType.UiError:
                {
                    var payload = _payloadBinder.Bind<UiErrorModel>(request.Payload);
                    _errorHandler.HandleUiError(payload.Source, payload.Type, payload.Message, payload.Stack);
                    return;
                }
            default:
                {
                    await _webViewClient.PostMessageAsync(new WebViewMessageResponse(
                        WebViewMessageType.Error,
                        $"Unknown message type: {request.Type}",
                        DateTime.Now
                    ));
                    return;
                }


        }
    }

    private async Task AskAiAssistantAsync(WebViewMessageRequest request)
    {
        _sendChatMessageUseCase = _chatUseCaseFactory.Create();

        var payload = _payloadBinder.Bind<ChatMessage>(request.Payload);

        //if (payload?.Stream == true)
        if (true)
        {
            await _sendChatMessageUseCase.ExecuteStreamingAsync(
                payload,
                false,
                async response =>
                {
                    await CheckIfTitleChangedAsync(response);

                    await _webViewClient.PostMessageAsync(response);
                });
        }
        else
        {
            var response = await _sendChatMessageUseCase.ExecuteAsync(payload, false);

            await CheckIfTitleChangedAsync(response);

            await _webViewClient.PostMessageAsync(response);
        }
    }

    private async Task CheckIfTitleChangedAsync(ChatResponse response)
    {
        if (response.Meta != null && response.Meta.TryGetValue("titleChanged", out var changed) && (bool)changed)
        {
            var chatListTask = _chatManager.GetAllChatsAsync();
            var currentChatTask = _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);

            await Task.WhenAll(chatListTask, currentChatTask);

            await _webViewClient.PostMessageAsync(new WebViewMessageResponse()
            {
                Type = WebViewMessageType.ChatTitleChanged,
                Payload = new
                {
                    Chats = new
                    {
                        ChatList = chatListTask?.Result,
                        Current = currentChatTask?.Result,
                    }
                },
                Timestamp = DateTime.Now
            });
        }
    }

    public async Task SendInitialDataAsync()
    {
        // Get all configured providers and their models from ProviderManager
        var providers = _providerManager.AllProviders;

        var chatListTask = _chatManager.GetAllChatsAsync();
        var currentChatTask = _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);

        await Task.WhenAll(chatListTask, currentChatTask);

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.InitData,
            Payload = new
            {
                Providers = new
                {
                    AvailableProviders = providers,
                    Current = _providerManager.ActiveProvider
                },
                Chats = new
                {
                    ChatList = chatListTask?.Result,
                    Current = currentChatTask?.Result
                },
                References = await _referenceManager.GetAllReferencesAsync(),
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendSelectedProviderDataAsync()
    {
        // Get all configured providers and their models from ProviderManager
        var provider = _providerManager.AllProviders.FirstOrDefault(p => p.IsEnabled);

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SelectProvider,
            Payload = new
            {
                provider = provider,
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendSelectedChatApprovedAsync()
    {
        var chat = await _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SelectChatApproved,
            Payload = new
            {
                Chat = chat,
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task EnsureActiveChatSessionAsync()
    {
        var chatList = await _chatManager.GetAllChatsAsync();

        // Try to find an existing "new chat"
        var newChat = chatList.FirstOrDefault(c => c.IsNewChat);

        if (newChat != null)
        {
            await _sessionService.LoadSessionAsync(newChat.Id);

            await SendNewChatMessageAsync(chatList, newChat);
            return;
        }

        // Otherwise create a new session
        await _sessionService.CreateNewSessionAsync(
            _providerManager.ActiveProvider.Id,
            _providerManager.ActiveModel.Id
        );

        var currentChat = await _chatManager.LoadChatAsync(
            _sessionService.ActiveSession.SessionId
        );

        chatList = await _chatManager.GetAllChatsAsync();

        await SendNewChatMessageAsync(chatList, currentChat);
    }

    /// <summary>
    /// Sends the NewChat message to the WebView.
    /// </summary>
    private async Task SendNewChatMessageAsync(object chatList, object currentChat)
    {
        var message = new WebViewMessageResponse
        {
            Type = WebViewMessageType.NewChat,
            Payload = new
            {
                Chats = new
                {
                    ChatList = chatList,
                    Current = currentChat
                },
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task DeleteChatSessionAsync()
    {
        _chatManager.DeleteChat(_sessionService.ActiveSession.SessionId);

        var chatList = await _chatManager.GetAllChatsAsync();

        if (chatList.Count == 0)
        {
            await EnsureActiveChatSessionAsync();
            return;
        }

        await _sessionService.LoadSessionAsync(chatList.ElementAt(0).Id);

        var currentChat = await _chatManager.LoadChatAsync(
            _sessionService.ActiveSession.SessionId
        );

        await SendNewChatMessageAsync(chatList, currentChat);
    }
}