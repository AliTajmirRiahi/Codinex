/**
 * events.js
 * Message type constants exchanged with the Code Changes review host.
 * Kept independent from Chat/js/constants/events.js so the two views stay decoupled.
 */
export const EVENTS = {
    CHANGESET_SHOW: 'CHANGESET_SHOW',
    CHANGESET_DECISION: 'CHANGESET_DECISION',
    CHANGESET_VIEW_READY: 'CHANGESET_VIEW_READY'
};
