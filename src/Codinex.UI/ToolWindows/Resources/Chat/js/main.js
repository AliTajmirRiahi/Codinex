/**
 * main.js
 * The central entry point for the WebView UI.
 * Responsible for bootstrapping the entire frontend.
 */
import { getState, subscribe, setLoading, setInputLoading, setProvider, setCurrentModel, setChatList, setCurrentChat, setGroupList, setCurrentGroup, setComposerController, setActiveDocument, setSettings, setChatBlocked, setAwaitingClarification, setSolutionDirectory, setSolutionName, upsertComposerReference, removeComposerReference } from '../js/state/appState.js';
import { $, togglePanelHidden } from './utils/dom.js';
import { enhanceNumberInputs } from './utils/numberStepper.js';
import { applyComposerDirection } from './utils/languageDirection.js';
import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { createMessageDispatcher } from '../../Shared/bridge/messageDispatcher.js';
import { initChatController } from './controllers/chatController.js';
import { chatView } from './views/chatView.js';
import { AskUserQuestionView } from './views/askUserQuestionView.js';
import { PromptSizeGuardView } from './views/promptSizeGuardView.js';
import { initManageModelsController } from './controllers/manageModelsController.js';
import { initAddProviderController } from './controllers/addProviderController.js';
import { initAboutController } from './controllers/aboutController.js';
import { initBugReportController } from './controllers/bugReportController.js';
import { initSettingsController } from './controllers/settingsController.js';
import { EVENTS } from '../js/constants/events.js';
import { validationService } from './services/validationService.js';
import { reportError } from '../../Shared/bridge/errorReporter.js'

// Register Custom Elements
import '../../Shared/components/codinex-icon.js';
import '../../Shared/components/codinex-image.js';
import { initContextMenu } from '../../Shared/components/contextMenu.js';
import { initSelectionToolbar } from '../../Shared/components/selectionToolbar.js';

window.addEventListener('error', (event) => {
    debugger;
    if (event.target && (event.target.src || event.target.href))
        reportError(`Failed to load resource: ${event.target.src || event.target.href}`, 'Network/Resource Error');
    else
        reportError(event.error || event.message, "window");
}, true);

window.addEventListener('unhandledrejection', (event) => {
    debugger;
    reportError(event.reason, "promise");
});


document.addEventListener('DOMContentLoaded', () => {

    setLoading(true);

    initContextMenu();
    initSelectionToolbar();
    enhanceNumberInputs();

    /**
     * Initialize Controllers
     */
    const chatController = initChatController(webViewTransport);

    const manageModelsController = initManageModelsController(webViewTransport);
    const addProviderController = initAddProviderController(webViewTransport);
    initAboutController();
    const bugReportController = initBugReportController();
    const settingsController = initSettingsController(webViewTransport);

    const askUserQuestionView = new AskUserQuestionView({
        container: $('#ask-user-question-panel'),
        onAnswer: ({ requestId, answers }) => {
            webViewTransport.send(EVENTS.ASK_USER_ANSWER, { requestId, answers });
            askUserQuestionView.hide();
            setAwaitingClarification(false);
        }
    });

    const promptSizeGuardView = new PromptSizeGuardView({
        container: $('#prompt-size-guard-panel'),
        onDecision: ({ requestId, proceed }) => {
            webViewTransport.send(EVENTS.PROMPT_SIZE_DECISION, { requestId, proceed });
            setAwaitingClarification(false);
        }
    });

    function getChatsPayload(payload) {
        const chats = payload?.chats || payload?.Chats;

        if (!chats) return null;

        return {
            chatList: chats.chatList || chats.ChatList,
            current: chats.current || chats.Current
        };
    }

    function applyChatsPayload(payload) {
        const chats = getChatsPayload(payload);

        if (!chats) return;

        if (chats.chatList) setChatList(chats.chatList);
        if (chats.current) setCurrentChat(chats.current);

        chatController.renderChatList();
    }

    /**
     * Setup message dispatcher
     * Routes incoming messages from .NET to handlers
     */
    const dispatcher = createMessageDispatcher({
        onInitData: (data) => {

            subscribe(() => {
                var state = getState();
                togglePanelHidden('#input-loading-screen', state.isInputLoading);
                togglePanelHidden('#chat-blocked-banner', state.isChatBlocked);
                // A body-level class (rather than toggling the shared 'disable' class directly)
                // avoids fighting with composerView.js's own draft-text-driven send-btn state.
                document.body.classList.toggle('chat-blocked', state.isChatBlocked);
                document.body.classList.toggle('awaiting-clarification', state.isAwaitingClarification);
            })

            const chatBlocked = data.chatBlocked ?? data.ChatBlocked ?? false;
            setChatBlocked(chatBlocked);

            setSolutionDirectory(data.solutionDirectory ?? data.SolutionDirectory);
            setSolutionName(data.solutionName ?? data.SolutionName);

            if (data.providers != null && data.providers.current) {
                setProvider(data.providers.current);
                chatController.renderCurrentProvider();
            }

            if (data.groups != null && data.groups.groupList) {
                setGroupList(data.groups.groupList);
                setCurrentGroup(data.groups.current);
                chatController.renderProjectList();
            }

            if (data.chats != null && data.chats.chatList) {
                setChatList(data.chats.chatList);
                setCurrentChat(data.chats.current)
                chatController.renderChatList();
                chatController.navigateToChat();
            }

            if (data.settings) {
                setSettings(data.settings);
            }

            if (data.references) {
                setComposerController(data.references);
                setActiveDocument(data.activeDocument);
                chatController.setComposerReferences();
                chatController.handleActiveDocument();
            }

            manageModelsController.updateUI(data.providers);
            settingsController.updateUI(
                data.settings,
                data.providers,
                data.workspaceSettings,
                data.sourceControl || data.SourceControl);

            // Get references to the loading screen and the main chat UI
            const loadingScreen = $('#loading-screen');
            const mainChatWrapper = $('#main-chat-wrapper');

            // Hide the loading screen and show the main chat UI once data is loaded
            if (loadingScreen && mainChatWrapper) {
                togglePanelHidden('#loading-screen', false);
                togglePanelHidden('#main-chat-wrapper', true);
            }

            setLoading(false);
        },

        onChangeModelSettingApproved: (payload) => {
            togglePanelHidden('#models-loading-screen', false);

            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;

            if (currentProvider)
                setProvider(currentProvider);

            if (providers)
                manageModelsController.updateUI(providers);

            manageModelsController.clearSettingsError();
            manageModelsController.closeProviderSettings();
            chatController.renderCurrentProvider();
        },
        onChangeModelSettingRejected: (payload) => {
            const message = payload.message || payload.Message || 'Settings could not be saved.';

            // Do NOT rebuild the settings panel here: the save was rejected, nothing was
            // persisted, and updateUI() would wipe the provider/model/API-key the user is
            // in the middle of fixing. Just release the spinner and show why, in place.
            setInputLoading(false);

            // Persistent error next to the Save button; the modal stays open so the user
            // can correct the key / top up credits and retry.
            manageModelsController.showSettingsError(message);

            // Also toast it for the case where this rejection came from switching models
            // straight from the chat input dropdown, with the settings panel closed.
            validationService.showError({ message, mode: 'toast' });
        },
        onProviderModelsRefreshed: (payload) => {
            const providers = payload.providers || payload.Providers;

            manageModelsController.updateUI(providers, payload.SelectedProviderId || payload.selectedProviderId);

            setInputLoading(false);
        },
        onSettingsSaved: (payload) => {
            const settings = payload.settings || payload.Settings;

            setSettings(settings);
            settingsController.updateUI(settings);
            chatController.handleSettingsChanged();
            settingsController.closeSettingsModal();
        },
        onSolutionInstructionSaved: (payload) => {
            const workspaceSettings = payload.workspaceSettings || payload.WorkspaceSettings;

            settingsController.updateUI(null, null, workspaceSettings);
        },
        onSelectModel: (payload) => {
            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;
            const activeModel = payload.activeModel || payload.ActiveModel;

            if (currentProvider) {
                setProvider(currentProvider);
            } else if (activeModel) {
                setCurrentModel(activeModel);
            }

            manageModelsController.updateUI(providers);
            chatController.renderCurrentProvider();
            setInputLoading(false);
        },
        onSelectChat: (payload) => {
            setCurrentChat(payload.chat);
            chatController.navigateToChat();
        },
        onSelectGroup: (payload) => {
            setGroupList(payload.groups.groupList);
            setCurrentGroup(payload.groups.current);
            setChatList(payload.chats.chatList);
            setCurrentChat(payload.chats.current);
            chatController.renderProjectList();
            chatController.renderChatList();
            chatController.navigateToProject();
        },

        onAIResponse: (payload, meta) => {
            chatController.handleAIResponse(payload, meta);

            if (meta && (meta.titleChanged || meta.TitleChanged)) {
                applyChatsPayload(meta);
            }
        },
        onHandleStreamChunk: (payload, meta) => {
            chatController.handleStreamChunk(payload, meta);
        },
        onThinkingStarted: () => {
            chatController.handleThinkingStarted();
        },
        onThinkingChunk: (payload) => {
            chatController.handleThinkingChunk(payload);
        },
        onThinkingCompleted: () => {
            chatController.handleThinkingCompleted();
        },
        onStatusChanged: (payload) => {
            chatController.handleStatusChanged(payload);
        },

        onChatTitleChanged: (payload) => {
            applyChatsPayload(payload);
        },
        onNewChat: (payload) => {
            applyChatsPayload(payload);
            chatController.navigateToChat();

            const forkText = payload?.forkText ?? payload?.ForkText;

            if (forkText !== undefined) {
                chatController.handleForkChatApproved(payload);
            }
        },  
        onActiveDocumentChanged: (payload) => {
            setActiveDocument(payload);
            chatController.handleActiveDocument();
        },
        onReferenceAdded: (payload) => {
            upsertComposerReference(payload);
            chatController.setComposerReferences();
        },
        onReferenceRemoved: (payload) => {
            const id = payload?.id ?? payload?.Id;
            removeComposerReference(id);
            chatController.setComposerReferences();
        },
        onReferenceUpdated: (payload) => {
            upsertComposerReference(payload);
            chatController.setComposerReferences();
        },
        onInputLanguageChanged: (payload) => {
            const isRightToLeft = payload?.isRightToLeft ?? payload?.IsRightToLeft ?? false;
            applyComposerDirection(isRightToLeft);
        },
        onError: (error) => {
            chatController.handleAIError(error);
        },
        onChatBlocked: () => {
            setChatBlocked(true);
        },
        onChatUnblocked: () => {
            setChatBlocked(false);
        },
        onAskUserQuestion: (payload) => {
            setAwaitingClarification(true);
            askUserQuestionView.show(payload);
        },
        onPromptSizeWarning: (payload) => {
            setAwaitingClarification(true);
            promptSizeGuardView.show(payload);
        },
        onRewindChatApproved: (payload) => {
            chatController.handleRewindChatApproved(payload);
        },
        onAddSelectedCodeReference: (payload) => {
            chatController.addSelectedCodeReference(payload);
        },
        onRunCommandOnSelection: (payload) => {
            chatController.runCommandOnSelection(payload);
        },
        onBugReportSubmitted: (payload) => {
            bugReportController.handleBugReportSubmitted(payload);
            chatView.handleBugReportSubmitted(payload);
        },
        onCustomProviderAdded: (payload) => {
            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;

            if (currentProvider) setProvider(currentProvider);
            if (providers) manageModelsController.updateUI(providers);

            manageModelsController.clearSettingsError();
            addProviderController.handleProviderAdded();
            chatController.renderCurrentProvider();
        },
        onCustomProviderAddRejected: (payload) => {
            const message = payload.message || payload.Message || 'Provider could not be added.';

            addProviderController.handleProviderAddRejected(message);
        },
        onCustomProviderUpdated: (payload) => {
            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;

            if (currentProvider) setProvider(currentProvider);
            if (providers) manageModelsController.updateUI(providers);

            manageModelsController.clearSettingsError();
            addProviderController.handleProviderUpdated();
            chatController.renderCurrentProvider();
        },
        onCustomProviderUpdateRejected: (payload) => {
            const message = payload.message || payload.Message || 'Provider could not be updated.';

            addProviderController.handleProviderUpdateRejected(message);
        }
    });

    /**
     * Listen for messages coming from .NET
     */
    webViewTransport.onMessage((data) => {
        dispatcher(data);
    });

    /**
     * Clicking the "pending review" banner re-shows the Code Changes window —
     * otherwise a user who closed it without deciding has no way back to it.
     */
    const chatBlockedBanner = $('#chat-blocked-banner');
    if (chatBlockedBanner) {
        const reopenPendingReview = () => webViewTransport.send(EVENTS.REOPEN_CHANGESET_REVIEW, {});

        chatBlockedBanner.addEventListener('click', reopenPendingReview);
        chatBlockedBanner.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                reopenPendingReview();
            }
        });
    }

    /**
     * Notify .NET backend that the UI is ready
     */
    webViewTransport.send(EVENTS.READY, {
        timestamp: Date.now()
    });

});
