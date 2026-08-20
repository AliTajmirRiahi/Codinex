// Self-contained, provider-agnostic component that renders the sequence of
// "thought" lines streamed into a single thinking-box turn as individually
// collapsible items, keeping only the most recent ones expanded inline and
// folding older ones into a collapsible "Total Thoughts (N)" group.
//
// A new thought boundary is a newline in the streamed text — each committed
// line becomes its own thought item, while the trailing (not yet
// newline-terminated) text is shown live as the current in-progress thought.
const MAX_VISIBLE_THOUGHTS = 3;

function createChevron() {
    const chevron = document.createElement('codinex-image');

    chevron.setAttribute('name', 'chevron-down.svg');
    chevron.className = 'thinking-chevron';

    return chevron;
}

function createThoughtItem(text) {
    const el = document.createElement('div');
    el.className = 'thought-item thought-item-enter';

    const toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'thought-item-toggle';
    toggle.setAttribute('aria-expanded', 'true');
    toggle.appendChild(createChevron());

    const label = document.createElement('span');
    label.className = 'thought-item-label';
    label.textContent = 'Thought';
    toggle.appendChild(label);

    const body = document.createElement('div');
    body.className = 'thought-item-text';
    body.textContent = text || '';

    toggle.addEventListener('click', () => {
        const collapsed = el.classList.toggle('collapsed');

        toggle.setAttribute('aria-expanded', String(!collapsed));
    });

    el.appendChild(toggle);
    el.appendChild(body);

    return { el, toggle, body };
}

export class ThoughtGroupView {
    constructor(containerEl) {
        this.container = containerEl;
        this.pendingText = '';
        this.visible = [];
        this.groupedCount = 0;

        this.groupEl = document.createElement('div');
        this.groupEl.className = 'thought-group collapsed hidden';

        this.groupToggle = document.createElement('button');
        this.groupToggle.type = 'button';
        this.groupToggle.className = 'thought-group-toggle';
        this.groupToggle.setAttribute('aria-expanded', 'false');
        this.groupToggle.appendChild(createChevron());

        this.groupLabel = document.createElement('span');
        this.groupLabel.className = 'thought-group-label';
        this.groupToggle.appendChild(this.groupLabel);

        this.groupList = document.createElement('div');
        this.groupList.className = 'thought-group-list';

        this.groupToggle.addEventListener('click', () => {
            const collapsed = this.groupEl.classList.toggle('collapsed');

            this.groupToggle.setAttribute('aria-expanded', String(!collapsed));
        });

        this.groupEl.appendChild(this.groupToggle);
        this.groupEl.appendChild(this.groupList);
        this.container.appendChild(this.groupEl);
    }

    _ensureActiveThought() {
        if (this.visible.length) return;

        const thought = createThoughtItem('');

        this.visible.push(thought);
        this.container.appendChild(thought.el);

        requestAnimationFrame(() => thought.el.classList.remove('thought-item-enter'));
    }

    _updateActiveText(text) {
        this._ensureActiveThought();

        const active = this.visible[this.visible.length - 1];

        active.body.textContent = text;
    }

    _commitLine(line) {
        const trimmed = line.trim();

        // Finalize the currently open thought with the committed line, then
        // start a brand-new one for whatever streams in next.
        this._updateActiveText(trimmed);

        if (!trimmed) return;

        this._appendVisible(createThoughtItem(''));
    }

    _appendVisible(thought) {
        this.visible.push(thought);
        thought.el.classList.add('thought-item-enter');
        this.container.appendChild(thought.el);

        requestAnimationFrame(() => thought.el.classList.remove('thought-item-enter'));

        while (this.visible.length > MAX_VISIBLE_THOUGHTS) {
            this._foldOldestIntoGroup();
        }
    }

    _foldOldestIntoGroup() {
        const oldest = this.visible.shift();

        if (!oldest) return;

        this.groupedCount += 1;
        this.groupEl.classList.remove('hidden');
        this._updateGroupLabel();

        // Move the existing DOM node (no teardown/rebuild) into the group,
        // collapsed by default, animating the fold.
        oldest.el.classList.add('thought-item-folding');
        oldest.el.classList.add('collapsed');
        oldest.toggle.setAttribute('aria-expanded', 'false');

        this.groupList.appendChild(oldest.el);

        requestAnimationFrame(() => oldest.el.classList.remove('thought-item-folding'));
    }

    _updateGroupLabel() {
        this.groupLabel.textContent = `Total Thoughts (${this.groupedCount})`;
    }

    /**
     * Feeds a raw streamed chunk into the current turn's thought sequence.
     * @param {string} chunk
     */
    appendChunk(chunk) {
        if (!chunk) return;

        this.pendingText += chunk;

        const parts = this.pendingText.split('\n');

        this.pendingText = parts.pop() ?? '';

        for (const line of parts) {
            this._commitLine(line);
        }

        this._updateActiveText(this.pendingText);
    }

    destroy() {
        this.container = null;
    }
}
