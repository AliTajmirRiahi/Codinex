/**
 * changeReviewController.js
 * Entry point for the Code Changes review view: renders a proposed workspace
 * change set (file tree, diff, summary) and reports the user's Accept/Reject
 * decision back to the host.
 */
import { webViewTransport } from '../../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../constants/events.js';
import { diffLines } from '../utils/lineDiff.js';

const pick = (obj, camelKey, pascalKey) =>
    obj?.[camelKey] ?? obj?.[pascalKey];

function normalizePayload(payload) {
    const files = pick(payload, 'files', 'Files') || [];

    return {
        id: pick(payload, 'id', 'Id'),
        summary: pick(payload, 'summary', 'Summary') || '',
        files: files.map(f => ({
            filePath: pick(f, 'filePath', 'FilePath') || '',
            operation: pick(f, 'operation', 'Operation') || '',
            originalText: pick(f, 'originalText', 'OriginalText') || '',
            modifiedText: pick(f, 'modifiedText', 'ModifiedText') || ''
        }))
    };
}

function renderFileList(container, files, selectedIndex, onSelect) {
    container.innerHTML = '';

    files.forEach((file, index) => {
        const item = document.createElement('div');
        item.className = 'file-item' + (index === selectedIndex ? ' active' : '');

        const path = document.createElement('span');
        path.className = 'file-item-path';
        path.textContent = file.filePath;
        path.title = file.filePath;

        const badge = document.createElement('span');
        badge.className = `file-item-badge op-${file.operation}`;
        badge.textContent = file.operation.replace(/File|Directory/, '');

        item.appendChild(path);
        item.appendChild(badge);
        item.addEventListener('click', () => onSelect(index));

        container.appendChild(item);
    });
}

function renderDiff(container, file) {
    container.innerHTML = '';

    if (!file) return;

    const lines = diffLines(file.originalText, file.modifiedText);

    const fragment = document.createDocumentFragment();
    let oldLineNo = 1;
    let newLineNo = 1;

    for (const line of lines) {
        const row = document.createElement('div');
        const lineNum = document.createElement('span');
        const text = document.createElement('span');

        lineNum.className = 'diff-line-num';
        text.className = 'diff-line-text';
        text.textContent = line.text;

        if (line.type === 'add') {
            row.className = 'diff-line diff-line-add';
            lineNum.textContent = newLineNo++;
        } else if (line.type === 'remove') {
            row.className = 'diff-line diff-line-remove';
            lineNum.textContent = oldLineNo++;
        } else {
            row.className = 'diff-line';
            lineNum.textContent = newLineNo;
            oldLineNo++;
            newLineNo++;
        }

        row.appendChild(lineNum);
        row.appendChild(text);
        fragment.appendChild(row);
    }

    container.appendChild(fragment);
}

function initChangeReviewController(transport) {
    const emptyState = document.getElementById('empty-state');
    const root = document.getElementById('review-root');
    const summaryEl = document.getElementById('review-summary');
    const changeSummaryEl = document.getElementById('change-summary');
    const fileListEl = document.getElementById('file-list');
    const diffContentEl = document.getElementById('diff-content');
    const diffFilePathEl = document.getElementById('diff-file-path');
    const diffFileOperationEl = document.getElementById('diff-file-operation');
    const acceptBtn = document.getElementById('accept-btn');
    const rejectBtn = document.getElementById('reject-btn');

    let currentChangeset = null;
    let selectedIndex = 0;

    function selectFile(index) {
        selectedIndex = index;

        renderFileList(fileListEl, currentChangeset.files, selectedIndex, selectFile);

        const file = currentChangeset.files[selectedIndex];

        diffFilePathEl.textContent = file?.filePath || '';
        diffFileOperationEl.textContent = file?.operation || '';

        renderDiff(diffContentEl, file);
    }

    function showChangeset(payload) {
        currentChangeset = normalizePayload(payload);
        selectedIndex = 0;

        emptyState.classList.add('hidden');
        root.classList.remove('hidden');

        summaryEl.textContent = currentChangeset.summary;
        changeSummaryEl.textContent = currentChangeset.summary;

        selectFile(0);
    }

    function sendDecision(approved) {
        if (!currentChangeset) return;

        if (transport.isAvailable()) {
            transport.send(EVENTS.CHANGESET_DECISION, {
                id: currentChangeset.id,
                approved
            });
        } else {
            // Standalone browser test mode: no WebView2 host to report back to.
            console.log('[Test Mode] Decision:', approved ? 'Accepted' : 'Rejected', currentChangeset.id);
        }

        currentChangeset = null;

        root.classList.add('hidden');
        emptyState.classList.remove('hidden');
    }

    acceptBtn.addEventListener('click', () => sendDecision(true));
    rejectBtn.addEventListener('click', () => sendDecision(false));

    transport.onMessage((message) => {
        if (!message || message.type !== EVENTS.CHANGESET_SHOW) return;

        showChangeset(message.payload);
    });

    if (transport.isAvailable()) {
        // Tell the host we're listening. PostWebMessageAsJson silently drops
        // messages sent before the page has a receiver, so the host waits for
        // this before posting CHANGESET_SHOW.
        transport.send(EVENTS.CHANGESET_VIEW_READY);
    } else {
        // Standalone browser test mode: no WebView2 host present, load a fixture
        // so the view can be exercised without Visual Studio or a real AI run.
        import('../../test/sampleChangeset.js').then(({ sampleChangeset }) => {
            showChangeset(sampleChangeset);
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    initChangeReviewController(webViewTransport);
});
