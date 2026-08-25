/**
 * messageDispatcher.js
 * Routes incoming messages from the bridge to specific application logic.
 */
import { EVENTS } from '../../Chat/js/constants/events.js'
export function createMessageDispatcher(handlers) {
    /**
     * The dispatcher function.
     * @param {Object} message - The raw message from .NET (type and payload).
     */
    return function dispatch(message) {
        if (!message || !message.type) {
            throw new Error("[Dispatcher] Invalid message format received from .NET");
        }

        const { type, payload } = message;

        switch (type) {
            case EVENTS.INIT_DATA:
                if (handlers.onInitData) handlers.onInitData(payload);
                break;

            case EVENTS.CHANGE_MODEL_SETTING_APPROVED:
                if (handlers.onChangeModelSettingApproved) handlers.onChangeModelSettingApproved(payload);
                break;

            case EVENTS.CHANGE_MODEL_SETTING_REJECTED:
                if (handlers.onChangeModelSettingRejected) handlers.onChangeModelSettingRejected(payload);
                break;

            case EVENTS.SELECT_MODEL_APPROVED:
                if (handlers.onSelectModel) handlers.onSelectModel(payload);
                break;

            case EVENTS.PROVIDER_MODELS_REFRESHED:
                if (handlers.onProviderModelsRefreshed) handlers.onProviderModelsRefreshed(payload);
                break;

            case EVENTS.SETTINGS_SAVED:
                if (handlers.onSettingsSaved) handlers.onSettingsSaved(payload);
                break;

            case EVENTS.SOLUTION_INSTRUCTION_SAVED:
                if (handlers.onSolutionInstructionSaved) handlers.onSolutionInstructionSaved(payload);
                break;

            case EVENTS.SELECT_CHAT_APPROVED:
                if (handlers.onSelectChat) handlers.onSelectChat(payload);
                break;

            case EVENTS.SELECT_GROUP_APPROVED:
                if (handlers.onSelectGroup) handlers.onSelectGroup(payload);
                break;

            case EVENTS.AI_RESPONSE:
                if (handlers.onAIResponse) handlers.onAIResponse(payload, message.meta || message.Meta);
                break;

            case EVENTS.STREAM_CHUNK:
                if (handlers.onHandleStreamChunk) handlers.onHandleStreamChunk(payload, message.meta || message.Meta);
                break;

            case EVENTS.THINKING_STARTED:
                if (handlers.onThinkingStarted) handlers.onThinkingStarted(payload, message.meta || message.Meta);
                break;

            case EVENTS.THINKING_CHUNK:
                if (handlers.onThinkingChunk) handlers.onThinkingChunk(payload, message.meta || message.Meta);
                break;

            case EVENTS.THINKING_COMPLETED:
                if (handlers.onThinkingCompleted) handlers.onThinkingCompleted(payload, message.meta || message.Meta);
                break;

            case EVENTS.STATUS_CHANGED:
                if (handlers.onStatusChanged) handlers.onStatusChanged(payload);
                break;

            case EVENTS.CHAT_TITLE_CHANGED:
                if (handlers.onChatTitleChanged) handlers.onChatTitleChanged(payload);
                break;

            case EVENTS.NEW_CHAT:
                if (handlers.onNewChat) handlers.onNewChat(payload);
                break;

            case EVENTS.ACTIVE_DOCUMENT_CHANGED:
                if (handlers.onActiveDocumentChanged) handlers.onActiveDocumentChanged(payload);
                break;

            case EVENTS.REFERENCE_ADDED:
                if (handlers.onReferenceAdded) handlers.onReferenceAdded(payload);
                break;

            case EVENTS.REFERENCE_REMOVED:
                if (handlers.onReferenceRemoved) handlers.onReferenceRemoved(payload);
                break;

            case EVENTS.REFERENCE_UPDATED:
                if (handlers.onReferenceUpdated) handlers.onReferenceUpdated(payload);
                break;

            case EVENTS.INPUT_LANGUAGE_CHANGED:
                if (handlers.onInputLanguageChanged) handlers.onInputLanguageChanged(payload);
                break;

            case EVENTS.ERROR:
                // Keep the full error payload so the chat controller can extract
                // the user-facing message from AiError/provider error shapes.
                if (handlers.onError) handlers.onError(payload);
                break;

            case EVENTS.CHAT_BLOCKED:
                if (handlers.onChatBlocked) handlers.onChatBlocked(payload);
                break;

            case EVENTS.CHAT_UNBLOCKED:
                if (handlers.onChatUnblocked) handlers.onChatUnblocked(payload);
                break;

            case EVENTS.ASK_USER_QUESTION:
                if (handlers.onAskUserQuestion) handlers.onAskUserQuestion(payload);
                break;

            case EVENTS.REWIND_CHAT_APPROVED:
                if (handlers.onRewindChatApproved) handlers.onRewindChatApproved(payload);
                break;

            case EVENTS.ADD_SELECTED_CODE_REFERENCE:
                if (handlers.onAddSelectedCodeReference) handlers.onAddSelectedCodeReference(payload);
                break;

            case EVENTS.RUN_COMMAND_ON_SELECTION:
                if (handlers.onRunCommandOnSelection) handlers.onRunCommandOnSelection(payload);
                break;

            case EVENTS.BUG_REPORT_SUBMITTED:
                if (handlers.onBugReportSubmitted) handlers.onBugReportSubmitted(payload);
                break;

            case EVENTS.CUSTOM_PROVIDER_ADDED:
                if (handlers.onCustomProviderAdded) handlers.onCustomProviderAdded(payload);
                break;

            case EVENTS.CUSTOM_PROVIDER_ADD_REJECTED:
                if (handlers.onCustomProviderAddRejected) handlers.onCustomProviderAddRejected(payload);
                break;

            case EVENTS.CUSTOM_PROVIDER_UPDATED:
                if (handlers.onCustomProviderUpdated) handlers.onCustomProviderUpdated(payload);
                break;

            case EVENTS.CUSTOM_PROVIDER_UPDATE_REJECTED:
                if (handlers.onCustomProviderUpdateRejected) handlers.onCustomProviderUpdateRejected(payload);
                break;

            default:
                throw new Error(`[Dispatcher] Unhandled message type: ${type}`);
        }
    };
}
