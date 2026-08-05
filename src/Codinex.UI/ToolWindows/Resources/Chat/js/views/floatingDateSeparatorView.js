const DAY_IN_MILLISECONDS = 24 * 60 * 60 * 1000;
const ISO_DATE_TIME_WITHOUT_TIMEZONE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?$/;
const EXPLICIT_TIMEZONE = /(?:z|[+-]\d{2}:?\d{2})$/i;

function startOfLocalDay(date) {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function getDateKey(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}

function parseDate(value) {
    if (!value) return null;

    if (value instanceof Date) {
        return isNaN(value.getTime()) ? null : value;
    }

    if (typeof value === 'string') {
        const trimmedValue = value.trim();
        const normalizedValue = ISO_DATE_TIME_WITHOUT_TIMEZONE.test(trimmedValue) && !EXPLICIT_TIMEZONE.test(trimmedValue)
            ? `${trimmedValue}Z`
            : trimmedValue;
        const date = new Date(normalizedValue);

        return isNaN(date.getTime()) ? null : date;
    }

    const date = new Date(value);

    return isNaN(date.getTime()) ? null : date;
}

export const defaultChatDateSeparatorFormatter = {
    format(date) {
        const messageDay = startOfLocalDay(date);
        const today = startOfLocalDay(new Date());
        const diffDays = Math.round((today.getTime() - messageDay.getTime()) / DAY_IN_MILLISECONDS);

        if (diffDays === 0) return 'Today';
        if (diffDays === 1) return 'Yesterday';

        return date.toLocaleDateString([], {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    }
};

/**
 * Presentation-only sticky date separator for the chat message list.
 * It observes dated message DOM nodes and renders an independent floating header.
 */
export class FloatingDateSeparatorView {
    constructor({ containerId, headerId, formatter }) {
        this.containerId = containerId;
        this.headerId = headerId;
        this.formatter = formatter || defaultChatDateSeparatorFormatter;
        this.entries = [];
        this.groups = [];
        this.activeDateKey = null;
        this.animationFrame = null;
        this.measureDirty = true;
        this.observer = null;
    }

    initialize() {
        this.container = document.getElementById(this.containerId);
        this.header = document.getElementById(this.headerId);

        if (!this.container || !this.header) return;

        this.headerLabel = this.header.querySelector('.floating-date-separator__label');
        this.headerInner = this.header.querySelector('.floating-date-separator__inner');

        this.container.addEventListener('scroll', () => this.requestUpdate(), { passive: true });
        window.addEventListener('resize', () => this.refresh(), { passive: true });

        this.observer = new MutationObserver(() => this.refresh());
        this.observer.observe(this.container, { childList: true });

        this.refresh();
    }

    refresh() {
        this.measureDirty = true;
        this.requestUpdate();
    }

    requestUpdate() {
        if (this.animationFrame) return;

        this.animationFrame = requestAnimationFrame(() => {
            this.animationFrame = null;
            this.update();
        });
    }

    update() {
        if (!this.container || !this.header) return;

        if (this.measureDirty) {
            this.measure();
            this.measureDirty = false;
        }

        if (this.entries.length === 0) {
            this.hide();
            return;
        }

        const headerHeight = this.headerInner?.offsetHeight || this.header.offsetHeight || 28;
        const activationTop = this.container.scrollTop + headerHeight + 8;
        const currentEntryIndex = this.findEntryIndexAt(activationTop);
        const currentEntry = this.entries[currentEntryIndex];

        if (!currentEntry) {
            this.hide();
            return;
        }

        const nextGroup = this.groups.find(group => group.entryIndex > currentEntryIndex);
        const pushDistance = nextGroup
            ? nextGroup.offsetTop - this.container.scrollTop - headerHeight - 8
            : Number.POSITIVE_INFINITY;
        const translateY = Math.min(0, pushDistance);

        this.show(currentEntry, translateY);
    }

    measure() {
        const elements = Array.from(this.container.querySelectorAll('[data-message-created-at]'));

        this.entries = elements
            .map(element => {
                const date = parseDate(element.dataset.messageCreatedAt);

                if (!date) return null;

                return {
                    element,
                    date,
                    dateKey: getDateKey(date),
                    label: this.formatter.format(date),
                    offsetTop: element.offsetTop
                };
            })
            .filter(Boolean)
            .sort((a, b) => a.offsetTop - b.offsetTop);

        this.groups = [];

        for (let index = 0; index < this.entries.length; index++) {
            const entry = this.entries[index];
            const previous = this.entries[index - 1];

            if (!previous || previous.dateKey !== entry.dateKey) {
                this.groups.push({
                    dateKey: entry.dateKey,
                    entryIndex: index,
                    offsetTop: entry.offsetTop
                });
            }
        }
    }

    findEntryIndexAt(scrollPosition) {
        let low = 0;
        let high = this.entries.length - 1;
        let result = 0;

        while (low <= high) {
            const middle = Math.floor((low + high) / 2);

            if (this.entries[middle].offsetTop <= scrollPosition) {
                result = middle;
                low = middle + 1;
            } else {
                high = middle - 1;
            }
        }

        return result;
    }

    show(entry, translateY) {
        if (this.activeDateKey !== entry.dateKey) {
            this.activeDateKey = entry.dateKey;

            if (this.headerLabel) {
                this.headerLabel.textContent = entry.label;
            }
        }

        if (this.headerInner) {
            this.headerInner.style.transform = `translateY(${translateY}px)`;
        }

        this.header.classList.add('visible');
    }

    hide() {
        this.activeDateKey = null;
        this.header.classList.remove('visible');

        if (this.headerInner) {
            this.headerInner.style.transform = 'translateY(0)';
        }
    }
}
