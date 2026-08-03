using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.UseCases;
using Codinex.Infrastructure.Chat;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Managers;
using Codinex.Storage.Models;
using Codinex.Storage.Models.DTO;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.References;

namespace Codinex.VisualStudio.WebView;

/// <summary>
/// Routes messages from WebView UI to application use cases.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class WebViewMessageRouter : IWebViewMessageRouter
{
    private readonly IWebViewClient _webViewClient;
    private readonly IJsonSerializer _serializer;
    private readonly IExecutionPipeline _pipeline;
    private readonly ProviderManager _providerManager;
    private readonly IPayloadBinder _payloadBinder;
    private readonly IChatUseCaseFactory _chatUseCaseFactory;
    private readonly ChatSessionService _sessionService;
    private readonly ChatManager _chatManager;
    private readonly IConversationGroupManager _conversationGroupManager;
    private readonly IErrorHandler _errorHandler;
    private readonly ReferenceManager _referenceManager;

    private ISendChatMessageUseCase _sendChatMessageUseCase;
    private CancellationTokenSource _generationCancellation;

    public WebViewMessageRouter(
        IExecutionPipeline pipeline,
        ProviderManager providerManager,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        IPayloadBinder payloadBinder,
        IChatUseCaseFactory chatUseCaseFactory,
        ChatSessionService sessionService,
        ChatManager chatManager,
        IConversationGroupManager conversationGroupManager,
        IErrorHandler errorHandler,
        ReferenceManager referenceManager)
    {
        _pipeline = pipeline;
        _providerManager = providerManager;
        _webViewClient = webViewClient;
        _serializer = serializer;
        _payloadBinder = payloadBinder;
        _chatUseCaseFactory = chatUseCaseFactory;
        _sessionService = sessionService;
        _chatManager = chatManager;
        _conversationGroupManager = conversationGroupManager;
        _errorHandler = errorHandler;
        _referenceManager = referenceManager;


        RegisterEventHandlers();
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

                    if (_providerManager.ActiveProvider != null)
                    {
                        await _sessionService.InitializeAsync();

                        await SendInitialDataAsync(true);

                        return;
                    }

                    await SendInitialDataAsync(false);
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
                    _generationCancellation?.Cancel();

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

                    await SendSelectedModelApprovedAsync();

                    return;
                }
            case WebViewMessageType.SelectChat:
                {
                    var payload = _payloadBinder.Bind<ChatSelectedDto>(request.Payload);

                    await _sessionService.LoadSessionAsync(payload.ChatId);

                    await SendSelectedChatApprovedAsync();

                    return;
                }

            case WebViewMessageType.SelectGroup:
                {
                    var payload = _payloadBinder.Bind<ConversationGroupSelectedDto>(request.Payload);

                    await _conversationGroupManager.SelectGroupAsync(payload.GroupId);
                    await _sessionService.InitializeAsync();

                    await SendSelectedGroupApprovedAsync();

                    return;
                }

            case WebViewMessageType.UpdateSettings:
                {
                    // Future: update provider settings
                    var aiProviderDto = _payloadBinder.Bind<AiProviderDto>(request.Payload);

                    await _providerManager.UpdateSettingsAsync(aiProviderDto);

                    await SendChangeModelSettingApprovedAsync();

                    return;
                }
            case WebViewMessageType.RefreshProviderModels:
                {
                    var providerId = request.Payload?["providerId"]?.ToString();

                    await _providerManager.RefreshModelsAsync(providerId);

                    await SendProviderModelsRefreshedAsync();

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
            case WebViewMessageType.NewGroup:
                {
                    var payload = _payloadBinder.Bind<ConversationGroupCreateDto>(request.Payload);

                    await CreateConversationGroupAsync(payload);

                    return;
                }
            case WebViewMessageType.UpdateGroup:
                {
                    var payload = _payloadBinder.Bind<ConversationGroupUpdateDto>(request.Payload);

                    await UpdateConversationGroupAsync(payload);

                    return;
                }
            case WebViewMessageType.DeleteGroup:
                {
                    var payload = _payloadBinder.Bind<ConversationGroupSelectedDto>(request.Payload);

                    await DeleteConversationGroupAsync(payload.GroupId);

                    return;
                }
            case WebViewMessageType.UiError:
                {
                    var payload = _payloadBinder.Bind<UiErrorModel>(request.Payload);
                    _errorHandler.HandleUiError(payload.Source, payload.Type, payload.Message, payload.Stack);
                    return;
                }
            case WebViewMessageType.OpenExternalLink:
                {
                    OpenExternalLink(request);
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

    private void RegisterEventHandlers()
    {
        _referenceManager.ActiveDocumentUpdated += (s, e) =>
        {
            _ = _pipeline.RunAsync(
                () => SendActiveDocumentAsync(e.ActiveDocument),
                nameof(SendActiveDocumentAsync));
        };
    }

    private void OpenExternalLink(WebViewMessageRequest request)
    {
        var url = request.Payload?["url"]?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps &&
            uri.Scheme != Uri.UriSchemeMailto)
            return;

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }

    private async Task AskAiAssistantAsync(WebViewMessageRequest request)
    {
        _generationCancellation?.Cancel();
        _generationCancellation?.Dispose();
        _generationCancellation = new CancellationTokenSource();

        var cancellationToken = _generationCancellation.Token;

        _sendChatMessageUseCase = _chatUseCaseFactory.Create();

        var payload = _payloadBinder.Bind<ChatMessageBuildRequest>(request.Payload);

        payload.ProjectInstruction = _conversationGroupManager.CurrentGroup?.Description ?? string.Empty;

        try
        {
            await _sendChatMessageUseCase.ExecuteStreamingAsync(
                payload,
                false,
                async response =>
                {
                    await CheckIfTitleChangedAsync(response);

                    await _webViewClient.PostMessageAsync(response);
                },
                cancellationToken);
        }
        finally
        {
            if (_generationCancellation != null)
            {
                _generationCancellation.Dispose();
                _generationCancellation = null;
            }
        }


        //if (payload?.Stream == true)
        //if (true)
        //{

        //}
        //else
        //{
        //    var response = await _sendChatMessageUseCase.ExecuteAsync(payload, false);

        //    await CheckIfTitleChangedAsync(response);

        //    await _webViewClient.PostMessageAsync(response);
        //}
    }

    private async Task CheckIfTitleChangedAsync(ChatResponse response)
    {
        if (response.Meta == null ||
            !response.Meta.TryGetValue("titleChanged", out var changed) ||
            changed is not bool titleChanged ||
            !titleChanged)
            return;

        var chatListTask = _chatManager.GetAllChatsAsync();
        var currentChatTask = _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);

        await Task.WhenAll(chatListTask, currentChatTask);

        var chats = new
        {
            ChatList = chatListTask.Result,
            Current = currentChatTask.Result,
        };

        response.Meta["chats"] = chats;

        await _webViewClient.PostMessageAsync(new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ChatTitleChanged,
            Payload = new
            {
                Chats = chats
            },
            Timestamp = DateTime.Now
        });
    }

    public async Task SendInitialDataAsync(bool includeChats = false)
    {
        // Get all configured providers
        var providers = _providerManager.Providers;

        Task<List<ChatSessionDocument>> chatListTask = null;
        Task<ChatSessionDocument> currentChatTask = null;
        var groupsTask = _conversationGroupManager.GetAllGroupsAsync();

        if (includeChats && _sessionService?.ActiveSession != null)
        {
            chatListTask = _chatManager.GetAllChatsAsync();
            currentChatTask = _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);
        }

        var referencesTask = _referenceManager.GetAllReferencesAsync();
        var activeDocumentTask = _referenceManager.GetActiveDocumentAsync();

        // Wait for all tasks that exist
        var tasks = new List<Task> { groupsTask, referencesTask, activeDocumentTask };
        if (chatListTask != null) tasks.Add(chatListTask);
        if (currentChatTask != null) tasks.Add(currentChatTask);

        await Task.WhenAll(tasks);

        var payload = new
        {
            Providers = new
            {
                AvailableProviders = providers,
                Current = _providerManager.ActiveProvider
            },
            Chats = includeChats
                ? new
                {
                    ChatList = chatListTask?.Result,
                    Current = currentChatTask?.Result
                }
                : null,
            Groups = new
            {
                GroupList = groupsTask.Result,
                Current = _conversationGroupManager.CurrentGroup
            },
            References = referencesTask?.Result,
            ActiveDocument = activeDocumentTask?.Result,
            Timestamp = DateTime.Now
        };

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.InitData,
            Payload = payload
        };

        await _webViewClient.PostMessageAsync(message);
    }


    public async Task SendChangeModelSettingApprovedAsync()
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ChangeModelSettingApproved,
            Payload = new
            {
                Providers = new
                {
                    AvailableProviders = _providerManager.Providers,
                    Current = _providerManager.ActiveProvider
                },
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendProviderModelsRefreshedAsync()
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ProviderModelsRefreshed,
            Payload = new
            {
                Providers = new
                {
                    AvailableProviders = _providerManager.Providers,
                    Current = _providerManager.ActiveProvider
                },
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }
    public async Task SendSelectedModelApprovedAsync()
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SelectModelApproved,
            Payload = new
            {
                ActiveModel = _providerManager.ActiveModel,
                Providers = new
                {
                    AvailableProviders = _providerManager.Providers,
                    Current = _providerManager.ActiveProvider
                },
                Timestamp = DateTime.Now
            }
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

    public async Task CreateConversationGroupAsync(ConversationGroupCreateDto payload)
    {
        var name = string.IsNullOrWhiteSpace(payload.Name)
            ? "New Project"
            : payload.Name.Trim();

        var description = payload.Description ?? string.Empty;

        await _conversationGroupManager.CreateGroupAsync(name, description);
        await _sessionService.InitializeAsync();

        await SendSelectedGroupApprovedAsync();
    }

    public async Task UpdateConversationGroupAsync(ConversationGroupUpdateDto payload)
    {
        if (payload.GroupId == Guid.Empty)
        {
            throw new InvalidOperationException("Conversation group id is required.");
        }

        var name = string.IsNullOrWhiteSpace(payload.Name)
            ? "New Project"
            : payload.Name.Trim();

        var description = payload.Description ?? string.Empty;

        await _conversationGroupManager.UpdateGroupAsync(new ConversationGroup
        {
            Id = payload.GroupId,
            Name = name,
            Description = description,
        });

        await SendSelectedGroupApprovedAsync();
    }

    public async Task DeleteConversationGroupAsync(Guid groupId)
    {
        await _conversationGroupManager.DeleteGroupAsync(groupId);
        await _sessionService.InitializeAsync();

        await SendSelectedGroupApprovedAsync();
    }

    public async Task SendSelectedGroupApprovedAsync()
    {
        var chatListTask = _chatManager.GetAllChatsAsync();
        var currentChatTask = _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);
        var groupsTask = _conversationGroupManager.GetAllGroupsAsync();

        await Task.WhenAll(chatListTask, currentChatTask, groupsTask);

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SelectGroupApproved,
            Payload = new
            {
                Groups = new
                {
                    GroupList = groupsTask.Result,
                    Current = _conversationGroupManager.CurrentGroup,
                },
                Chats = new
                {
                    ChatList = chatListTask.Result,
                    Current = currentChatTask.Result,
                },
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

    public async Task SendActiveDocumentAsync(ReferenceItem activeDocument)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ActiveDocumentChanged,
            Payload = activeDocument
        };

        await _webViewClient.PostMessageAsync(message);
    }

}