/**
 * askUserQuestionView.js
 * Renders the "ask_user_question" tool's clarifying questions as a stacked
 * carousel above the composer: one card centered and active, the rest fanned
 * out behind it (scaled down, faded). Navigation is arrow-only — no scroll
 * or drag. A trailing card lets the user type their own answer instead of
 * picking a suggestion.
 */
import { createElement } from '../utils/dom.js';
import { isRtlText } from '../utils/languageDirection.js';

const MAX_VISIBLE_OFFSET = 3;
const OFFSET_STEP_PX = 34;

export class AskUserQuestionView {
    constructor({ container, onAnswer }) {
        this.container = container;
        this.onAnswer = onAnswer;

        this.requestId = null;
        this.questions = [];
        this.answers = [];
        this.currentIndex = 0;
        this.activeCardIndex = 0;
        this.cardElements = [];
    }

    show(payload) {
        this.requestId = payload.requestId ?? payload.RequestId;
        this.questions = (payload.questions ?? payload.Questions ?? []).map((q) => ({
            header: q.header ?? q.Header ?? '',
            question: q.question ?? q.Question ?? '',
            options: (q.options ?? q.Options ?? []).map((o) => ({
                label: o.label ?? o.Label ?? '',
                description: o.description ?? o.Description ?? '',
                recommended: !!(o.recommended ?? o.Recommended)
            }))
        }));
        this.answers = [];
        this.currentIndex = 0;

        if (!this.container || this.questions.length === 0) return;

        this.container.classList.remove('hidden');
        this.renderCurrentQuestion();
    }

    hide() {
        if (!this.container) return;

        this.container.classList.add('hidden');
        this.container.innerHTML = '';
        this.requestId = null;
        this.questions = [];
        this.answers = [];
        this.currentIndex = 0;
        this.activeCardIndex = 0;
        this.cardElements = [];
    }

    renderCurrentQuestion() {
        const question = this.questions[this.currentIndex];
        if (!question) return;

        this.container.innerHTML = '';
        this.activeCardIndex = 0;
        this.cardElements = [];

        // Direction is derived from the question's own text, not the OS keyboard layout —
        // this content is AI-generated, not typed by the user. The stage (arrows/track) is
        // deliberately left untouched by this (see CSS): only the header and card text flip,
        // so the arrows always keep their fixed left/right meaning and never invert.
        const isRtl = isRtlText(question.question) ||
            isRtlText(question.header) ||
            question.options.some((o) => isRtlText(o.label) || isRtlText(o.description));

        const panel = createElement('div', `ask-user-question${isRtl ? ' ask-user-question--rtl' : ''}`);

        const header = createElement('div', 'ask-user-question__header');
        header.dir = isRtl ? 'rtl' : 'ltr';
        const progress = this.questions.length > 1
            ? `<span class="ask-user-question__progress">${this.currentIndex + 1}/${this.questions.length}</span>`
            : '';
        header.innerHTML = `
            <div class="ask-user-question__title">
                <codinex-icon name="message-circle-check"></codinex-icon>
                <span>${escapeHtml(question.question)}</span>
            </div>
            ${progress}
        `;
        panel.appendChild(header);

        const stage = createElement('div', 'ask-user-question__stage');

        const prevBtn = createElement('button', 'ask-user-question__nav-btn ask-user-question__nav-btn--prev');
        prevBtn.type = 'button';
        prevBtn.title = 'Previous answer';
        prevBtn.innerHTML = '<codinex-icon name="chevron-down"></codinex-icon>';
        prevBtn.addEventListener('click', () => this.navigate(-1));

        const track = createElement('div', 'ask-user-question__track');

        question.options.forEach((option, index) => {
            const card = this.createOptionCard(option, index, isRtl);
            track.appendChild(card);
            this.cardElements.push(card);
        });

        const customCard = this.createCustomCard(isRtl);
        track.appendChild(customCard);
        this.cardElements.push(customCard);

        const nextBtn = createElement('button', 'ask-user-question__nav-btn ask-user-question__nav-btn--next');
        nextBtn.type = 'button';
        nextBtn.title = 'Next answer';
        nextBtn.innerHTML = '<codinex-icon name="chevron-down"></codinex-icon>';
        nextBtn.addEventListener('click', () => this.navigate(1));

        stage.appendChild(prevBtn);
        stage.appendChild(track);
        stage.appendChild(nextBtn);
        panel.appendChild(stage);

        this.container.appendChild(panel);

        this.updateStack();
    }

    navigate(direction) {
        const next = this.activeCardIndex + direction;
        if (next < 0 || next >= this.cardElements.length) return;

        this.activeCardIndex = next;
        this.updateStack();
    }

    updateStack() {
        this.cardElements.forEach((card, index) => {
            const offset = index - this.activeCardIndex;
            const absOffset = Math.abs(offset);
            const isActive = offset === 0;

            card.classList.toggle('ask-user-question__card--active', isActive);

            if (absOffset > MAX_VISIBLE_OFFSET) {
                card.style.opacity = '0';
                card.style.pointerEvents = 'none';
                card.style.transform = `translate(-50%, -50%) translateX(${offset > 0 ? '999px' : '-999px'})`;
                card.style.zIndex = '0';
                return;
            }

            const scale = 1 - absOffset * 0.1;
            const opacity = isActive ? 1 : Math.max(0.18, 0.6 - absOffset * 0.15);
            const shiftPx = offset * OFFSET_STEP_PX + (offset === 0 ? 0 : Math.sign(offset) * 40);

            card.style.opacity = String(opacity);
            card.style.pointerEvents = isActive ? 'auto' : 'none';
            card.style.transform = `translate(-50%, -50%) translateX(${shiftPx}px) scale(${scale})`;
            card.style.zIndex = String(100 - absOffset);
        });
    }

    createOptionCard(option, index, isRtl) {
        const card = createElement('div', `ask-user-question__card${option.recommended ? ' ask-user-question__card--recommended' : ''}`);
        card.setAttribute('role', 'button');
        card.setAttribute('tabindex', '0');
        card.dir = isRtl ? 'rtl' : 'ltr';

        card.innerHTML = `
            <div class="ask-user-question__card-number">${index + 1}</div>
            <div class="ask-user-question__card-body">${escapeHtml(option.description || option.label)}</div>
            <div class="ask-user-question__card-footer">
                <codinex-icon name="${option.recommended ? 'message-circle-check' : 'message-square-plus'}"></codinex-icon>
                <span>${option.recommended ? 'Recommended' : 'Alternative'}</span>
            </div>
        `;

        const select = () => this.selectOption(option.label);
        card.addEventListener('click', select);
        card.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                select();
            }
        });

        return card;
    }

    createCustomCard(isRtl) {
        const card = createElement('div', 'ask-user-question__card ask-user-question__card--custom');
        card.dir = isRtl ? 'rtl' : 'ltr';

        card.innerHTML = `
            <div class="ask-user-question__card-footer ask-user-question__card-footer--top">
                <codinex-icon name="pencil"></codinex-icon>
                <span>Your own answer</span>
            </div>
            <textarea class="ask-user-question__custom-input" rows="3" placeholder="Type your answer..."></textarea>
            <button type="button" class="ask-user-question__custom-submit">Submit</button>
        `;

        const textarea = card.querySelector('.ask-user-question__custom-input');
        textarea.dir = isRtl ? 'rtl' : 'ltr';
        const submitBtn = card.querySelector('.ask-user-question__custom-submit');

        const submit = () => {
            const value = textarea.value.trim();
            if (!value) return;
            this.selectOption(value);
        };

        submitBtn.addEventListener('click', submit);
        textarea.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                submit();
            }
        });

        return card;
    }

    selectOption(answerText) {
        const question = this.questions[this.currentIndex];
        if (!question) return;

        this.answers.push({ question: question.question, answer: answerText });

        this.currentIndex += 1;

        if (this.currentIndex >= this.questions.length) {
            const requestId = this.requestId;
            const answers = this.answers;
            this.onAnswer?.({ requestId, answers });
            return;
        }

        this.renderCurrentQuestion();
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text ?? '';
    return div.innerHTML;
}
