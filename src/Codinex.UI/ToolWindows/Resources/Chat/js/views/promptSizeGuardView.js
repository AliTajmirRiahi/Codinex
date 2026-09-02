/**
 * promptSizeGuardView.js
 * Renders the "prompt is very large" confirmation box above the composer when an
 * outgoing request exceeds the configured payload-size threshold. Two actions:
 * Stop (abort the send) and Continue (send anyway). The host blocks the send until
 * one is chosen.
 */
import { createElement } from '../utils/dom.js';

export class PromptSizeGuardView {
    constructor({ container, onDecision }) {
        this.container = container;
        this.onDecision = onDecision;
        this.requestId = null;
    }

    show(payload) {
        this.requestId = payload?.requestId ?? payload?.RequestId ?? null;

        const sizeKb = payload?.sizeKb ?? payload?.SizeKb ?? 0;

        if (!this.container || !this.requestId) return;

        this.container.innerHTML = '';
        this.container.classList.remove('hidden');

        const panel = createElement('div', 'prompt-size-guard');
        panel.setAttribute('role', 'alertdialog');
        panel.innerHTML = `
            <div class="prompt-size-guard__icon">⚠️</div>
            <div class="prompt-size-guard__title">Your prompt is getting large</div>
            <div class="prompt-size-guard__body">
                The current prompt size is ${escapeHtml(String(sizeKb))} KB, which may lead to
                high token usage and increased costs.
            </div>
            <div class="prompt-size-guard__question">
                Do you want to continue sending this prompt to the model?
            </div>
            <div class="prompt-size-guard__actions">
                <button type="button" class="prompt-size-guard__btn prompt-size-guard__btn--stop">Stop</button>
                <button type="button" class="prompt-size-guard__btn prompt-size-guard__btn--continue">Continue</button>
            </div>
        `;

        panel.querySelector('.prompt-size-guard__btn--stop')
            .addEventListener('click', () => this.decide(false));
        panel.querySelector('.prompt-size-guard__btn--continue')
            .addEventListener('click', () => this.decide(true));

        this.container.appendChild(panel);
    }

    decide(proceed) {
        const requestId = this.requestId;

        this.hide();

        if (requestId) {
            this.onDecision?.({ requestId, proceed });
        }
    }

    hide() {
        this.requestId = null;

        if (!this.container) return;

        this.container.classList.add('hidden');
        this.container.innerHTML = '';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text ?? '';
    return div.innerHTML;
}
