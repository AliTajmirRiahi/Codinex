import { webViewTransport } from '../../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../constants/events.js';
import { $ } from '../utils/dom.js';
import { getState } from '../state/appState.js';

export function initBugReportController() {
    const reportBugButton = $('#report-bug-menu-btn');
    const modal = $('#bug-report-modal');
    const closeButton = $('#close-bug-report-modal');
    const cancelButton = $('#cancel-bug-report-btn');
    const submitButton = $('#submit-bug-report-btn');
    const descriptionInput = $('#bug-report-description');
    const statusEl = $('#bug-report-status');

    const setStatus = (message, isError) => {
        if (!statusEl)
            return;

        statusEl.textContent = message || '';
        statusEl.classList.toggle('hidden', !message);
        statusEl.classList.toggle('bug-report-status-error', !!isError);
        statusEl.classList.toggle('bug-report-status-success', !!message && !isError);
    };

    const setPending = (pending) => {
        if (submitButton) {
            submitButton.disabled = pending;
            submitButton.textContent = pending ? 'Sending…' : 'Submit';
        }
    };

    const openModal = () => {
        setStatus('');
        setPending(false);
        modal?.classList.remove('hidden');
    };

    const closeModal = () => {
        modal?.classList.add('hidden');
    };

    const submitReport = () => {
        const description = descriptionInput?.value?.trim();

        if (!description) {
            setStatus('Please describe what happened.', true);
            return;
        }

        const currentChat = getState().currentChat;
        const chatId = currentChat?.id || currentChat?.Id || null;

        setPending(true);
        setStatus('Sending report…', false);

        webViewTransport.send(EVENTS.SUBMIT_BUG_REPORT, {
            chatId,
            description
        });
    };

    reportBugButton?.addEventListener('click', openModal);
    closeButton?.addEventListener('click', closeModal);
    cancelButton?.addEventListener('click', closeModal);
    submitButton?.addEventListener('click', submitReport);

    modal?.addEventListener('click', (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    return {
        handleBugReportSubmitted(payload) {
            setPending(false);

            const success = payload?.success ?? payload?.Success;
            const message = payload?.message ?? payload?.Message;

            if (success) {
                setStatus(message || 'Bug report sent. Thank you!', false);

                if (descriptionInput) descriptionInput.value = '';

                return;
            }

            setStatus(message || 'Failed to send bug report.', true);
        }
    };
}
