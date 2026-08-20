using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using EnvDTE;
using Codinex.VisualStudio.Tools.BuiltIn.Clarification;
using Codinex.VisualStudio.Tools.BuiltIn.Workspace;
using Codinex.VisualStudio.SourceControl;
using Process = System.Diagnostics.Process;

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
    private readonly SettingsManager _settingsManager;
    private readonly IWorkspaceSettingsManager _workspaceSettingsManager;
    private readonly IVisualStudioServices _visualStudio;
    private readonly IWorkspaceFileService _workspaceFileService;
    private readonly IInputLanguageWatcher _inputLanguageWatcher;
    private readonly IChangesetSessionService _changesetSessionService;
    private readonly IClarificationSessionService _clarificationSessionService;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ISourceControlStatusService _sourceControlStatusService;

    private ISendChatMessageUseCase _sendChatMessageUseCase;
    private CancellationTokenSource _generationCancellation;

    public WebViewMessageRouter(
        IExecutionPipeline pipeline,
        ProviderManager providerManager,
        SettingsManager settingsManager,
        IWorkspaceSettingsManager workspaceSettingsManager,
        ChatSessionService sessionService,
        ChatManager chatManager,
        ReferenceManager referenceManager,
        IWebViewClient webViewClient,
        IJsonSerializer serializer,
        IPayloadBinder payloadBinder,
        IChatUseCaseFactory chatUseCaseFactory,
        IConversationGroupManager conversationGroupManager,
        IErrorHandler errorHandler,
        IVisualStudioServices visualStudio,
        IWorkspaceFileService workspaceFileService,
        IInputLanguageWatcher inputLanguageWatcher,
        IChangesetSessionService changesetSessionService,
        IClarificationSessionService clarificationSessionService,
        IWorkspaceContext workspaceContext,
        ISourceControlStatusService sourceControlStatusService)
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
        _settingsManager = settingsManager;
        _workspaceSettingsManager = workspaceSettingsManager;
        _visualStudio = visualStudio;
        _workspaceFileService = workspaceFileService;
        _inputLanguageWatcher = inputLanguageWatcher;
        _changesetSessionService = changesetSessionService;
        _clarificationSessionService = clarificationSessionService;
        _workspaceContext = workspaceContext;
        _sourceControlStatusService = sourceControlStatusService;


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
                    await _changesetSessionService.TryRestorePendingReviewAsync(CancellationToken.None);

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
                    if (_changesetSessionService.HasPending)
                    {
                        // Re-show the pending review rather than just refusing — otherwise a user who
                        // closed it without deciding has no way back to it.
                        await _changesetSessionService.ReopenPendingReviewAsync(cancellationToken: default);
                        await SendChatBlockedAsync();
                        return;
                    }

                    await AskAiAssistantAsync(request);

                    return;
                }

            case WebViewMessageType.ReopenChangesetReview:
                {
                    await _changesetSessionService.ReopenPendingReviewAsync(cancellationToken: default);
                    return;
                }

            case WebViewMessageType.AskUserAnswer:
                {
                    var payload = _payloadBinder.Bind<AskUserAnswerDto>(request.Payload);

                    _clarificationSessionService.SubmitAnswers(payload.RequestId, payload.Answers);

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
                    var aiProviderDto = _payloadBinder.Bind<AiProviderDto>(request.Payload);

                    try
                    {
                        var result = await _providerManager.UpdateSettingsAsync(aiProviderDto);

                        if (!result.Success)
                        {
                            await SendChangeModelSettingRejectedAsync(result.Message, result.IsAvailable);
                            return;
                        }

                        await SendChangeModelSettingApprovedAsync(result.Message);
                    }
                    catch (Exception ex)
                    {
                        await SendChangeModelSettingRejectedAsync(ex.Message, false);
                    }

                    return;
                }
            case WebViewMessageType.RefreshProviderModels:
                {
                    var providerId = request.Payload?["providerId"]?.ToString();

                    await _providerManager.RefreshModelsAsync(providerId);

                    await SendProviderModelsRefreshedAsync(providerId);

                    return;
                }
            case WebViewMessageType.SaveSettings:
                {
                    var payload = _payloadBinder.Bind<CodinexSettings>(request.Payload);

                    if (payload.ByPassPreviewChangeAndApplyChangeDirectly &&
                        !await _sourceControlStatusService.IsSolutionUnderSourceControlAsync(CancellationToken.None))
                    {
                        payload.ByPassPreviewChangeAndApplyChangeDirectly = false;
                    }

                    await _settingsManager.SaveSettingsAsync(payload);

                    await SendSettingsSavedAsync();

                    return;
                }
            case WebViewMessageType.SaveSolutionInstruction:
                {
                    var payload = _payloadBinder.Bind<SolutionInstructionDto>(request.Payload);

                    await _workspaceSettingsManager.SaveAsync(new WorkspaceSettings
                    {
                        SolutionInstruction = payload.SolutionInstruction ?? string.Empty,
                        ExcludeDirectories = payload.ExcludeDirectories ?? string.Empty,
                        ExcludeFiles = payload.ExcludeFiles ?? string.Empty,
                        IgnoredExtensions = payload.IgnoredExtensions ?? string.Empty,
                        IgnoredFileSuffixes = payload.IgnoredFileSuffixes ?? string.Empty
                    });

                    await SendSolutionInstructionSavedAsync();

                    return;
                }
            case WebViewMessageType.NewChat:
                {
                    await EnsureActiveChatSessionAsync();
                    return;
                }
            case WebViewMessageType.UpdateChat:
                {
                    var payload = _payloadBinder.Bind<ChatUpdateDto>(request.Payload);

                    await UpdateChatSessionAsync(payload);
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
            case WebViewMessageType.RewindChat:
                {
                    var payload = _payloadBinder.Bind<RewindChatDto>(request.Payload);

                    await RewindChatAsync(payload.MessageIndex);

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
            case WebViewMessageType.OpenReferenceFile:
                {
                    await OpenReferenceFileAsync(request);
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

        _inputLanguageWatcher.InputLanguageChanged += (s, e) =>
        {
            _ = _pipeline.RunAsync(
                () => SendInputLanguageChangedAsync(e),
                nameof(SendInputLanguageChangedAsync));
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
#pragma warning disable VSTHRD010

    private async Task OpenReferenceFileAsync(WebViewMessageRequest request)
    {
        var filePath = request.Payload?["filePath"]?.ToString();
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (!_workspaceFileService.FileExists(filePath))
            return;

        var dte = await _visualStudio.GetDteAsync();
        dte?.ItemOperations.OpenFile(filePath);

        var startLine = request.Payload?["startLine"]?.ToObject<int?>();
        var endLine = request.Payload?["endLine"]?.ToObject<int?>();

        if (startLine is null or <= 0)
            return;

        if (dte?.ActiveDocument?.Selection is not TextSelection selection)
            return;

        selection.MoveToLineAndOffset(startLine.Value, 1, false);
        selection.MoveToLineAndOffset(endLine ?? startLine.Value, 1, true);
        selection.EndOfLine(true);
    }
#pragma warning restore VSTHRD010

    private async Task AskAiAssistantAsync(WebViewMessageRequest request)
    {
        _generationCancellation?.Cancel();
        _generationCancellation?.Dispose();
        _generationCancellation = new CancellationTokenSource();

        var cancellationToken = _generationCancellation.Token;

        _sendChatMessageUseCase = _chatUseCaseFactory.Create();

        var payload = _payloadBinder.Bind<ChatMessageBuildRequest>(request.Payload);

        payload.ProjectName = _conversationGroupManager.CurrentGroup?.Name ?? string.Empty;
        payload.ProjectInstruction = _conversationGroupManager.CurrentGroup?.Description ?? string.Empty;
        payload.SolutionInstruction = _workspaceSettingsManager.Settings?.SolutionInstruction ?? string.Empty;

        var canStream = _providerManager.ActiveModel.SupportsStreaming == CapabilityProbeResult.Supported
                         && _settingsManager.Settings.EnableStreamingChat;

        try
        {
            if (canStream)
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
            else
            {
                var response = await _sendChatMessageUseCase.ExecuteAsync(payload, false, cancellationToken);

                await CheckIfTitleChangedAsync(response);

                await _webViewClient.PostMessageAsync(response);
            }
        }
        finally
        {
            if (_generationCancellation != null)
            {
                _generationCancellation.Dispose();
                _generationCancellation = null;
            }
        }
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
        var isSolutionUnderSourceControl = await _sourceControlStatusService.IsSolutionUnderSourceControlAsync(CancellationToken.None);

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
            Settings = _settingsManager.Settings,
            WorkspaceSettings = _workspaceSettingsManager.Settings,
            ChatBlocked = _changesetSessionService.HasPending,
            SolutionDirectory = _workspaceContext.SolutionDirectory,
            SolutionName = _workspaceContext.SolutionName,
            SourceControl = new
            {
                IsSolutionUnderSourceControl = isSolutionUnderSourceControl
            },
            Timestamp = DateTime.Now
        };

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.InitData,
            Payload = payload
        };

        await _webViewClient.PostMessageAsync(message);
    }

    /// <summary>
    /// Reminds the UI that chat is locked because a changeset review is pending, in case a
    /// message somehow got sent anyway (e.g. one already in flight when the block began).
    /// </summary>
    private Task SendChatBlockedAsync()
    {
        return _webViewClient.PostMessageAsync(new WebViewMessageResponse
        {
            Type = WebViewMessageType.ChatBlocked,
            Payload = new { }
        });
    }


    public async Task SendChangeModelSettingApprovedAsync(string messageText = null)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ChangeModelSettingApproved,
            Payload = new
            {
                Message = messageText,
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

    public async Task SendChangeModelSettingRejectedAsync(string messageText, bool isAvailable)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ChangeModelSettingRejected,
            Payload = new
            {
                Message = messageText,
                IsAvailable = isAvailable,
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

    public async Task SendProviderModelsRefreshedAsync(string selectedProviderId)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.ProviderModelsRefreshed,
            Payload = new
            {
                SelectedProviderId = selectedProviderId,
                Providers = new
                {
                    AvailableProviders = _providerManager.Providers,
                },
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendSettingsSavedAsync()
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SettingsSaved,
            Payload = new
            {
                Settings = _settingsManager.Settings,
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }
    public async Task SendSolutionInstructionSavedAsync()
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.SolutionInstructionSaved,
            Payload = new
            {
                WorkspaceSettings = _workspaceSettingsManager.Settings,
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

    public async Task UpdateChatSessionAsync(ChatUpdateDto payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.ChatId))
            return;

        var title = string.IsNullOrWhiteSpace(payload.Title)
            ? string.Empty
            : payload.Title.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return;

        if (title.Length > 25)
            title = title.Substring(0, 25);

        var chat = await _chatManager.LoadChatAsync(payload.ChatId);

        if (chat == null || chat.IsNewChat)
            return;

        await _chatManager.UpdateChatTitleAsync(payload.ChatId, title);

        var chatList = await _chatManager.GetAllChatsAsync();
        var currentChat = await _chatManager.LoadChatAsync(_sessionService.ActiveSession.SessionId);

        await SendChatTitleChangedMessageAsync(chatList, currentChat);
    }

    private async Task SendChatTitleChangedMessageAsync(object chatList, object currentChat)
    {
        var message = new WebViewMessageResponse
        {
            Type = WebViewMessageType.ChatTitleChanged,
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

    /// <summary>
    /// Deletes the given user message and every message after it from the active chat,
    /// then hands the deleted user message's text back to the UI so it can be restored
    /// into the composer for editing/resending.
    /// </summary>
    public async Task RewindChatAsync(int messageIndex)
    {
        var chatId = _sessionService.ActiveSession.SessionId;
        var chat = await _chatManager.LoadChatAsync(chatId);

        if (chat == null || messageIndex < 0 || messageIndex >= chat.Messages.Count)
            return;

        var targetMessage = chat.Messages[messageIndex];

        if (!string.Equals(targetMessage.Role, "user", StringComparison.OrdinalIgnoreCase))
            return;

        var rewindText = targetMessage.Content;
        var rewindReferences = targetMessage.Context?.SelectedReferences;

        chat.Messages = chat.Messages.Take(messageIndex).ToList();

        await _chatManager.SaveChatAsync(chat);

        await _sessionService.LoadSessionAsync(chatId);

        var message = new WebViewMessageResponse
        {
            Type = WebViewMessageType.RewindChatApproved,
            Payload = new
            {
                Chat = chat,
                RewindText = rewindText,
                RewindReferences = rewindReferences,
                Timestamp = DateTime.Now
            }
        };

        await _webViewClient.PostMessageAsync(message);
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

    public Task SendSelectedCodeReferenceAsync(ReferenceItem selection)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.AddSelectedCodeReference,
            Payload = selection,
            Timestamp = DateTime.Now
        };

        return _webViewClient.PostMessageAsync(message);
    }

    public async Task RunCommandOnSelectionAsync(ReferenceItem selection, string commandName)
    {
        var defaultGroup = await _conversationGroupManager.CreateDefaultGroupAsync();

        if (_conversationGroupManager.CurrentGroup?.Id != defaultGroup.Id)
        {
            await _conversationGroupManager.SelectGroupAsync(defaultGroup.Id);
            await _sessionService.InitializeAsync();
        }

        await EnsureActiveChatSessionAsync();

        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.RunCommandOnSelection,
            Payload = new
            {
                Selection = selection,
                CommandName = commandName
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }

    public async Task SendInputLanguageChangedAsync(InputLanguageChangedEventArgs inputLanguage)
    {
        var message = new WebViewMessageResponse()
        {
            Type = WebViewMessageType.InputLanguageChanged,
            Payload = new
            {
                LanguageTag = inputLanguage.LanguageTag,
                LanguageName = inputLanguage.LanguageName,
                IsRightToLeft = inputLanguage.IsRightToLeft
            },
            Timestamp = DateTime.Now
        };

        await _webViewClient.PostMessageAsync(message);
    }

}