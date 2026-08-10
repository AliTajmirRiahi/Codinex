/**
 * changeReviewController.js
 * Entry point for the Code Changes review view: renders a proposed workspace
 * change set (file tree, diff, summary) and reports the user's Accept/Reject
 * decision back to the host.
 */
import { webViewTransport } from '../../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../constants/events.js';
import { diffLines } from '../utils/lineDiff.js';
import { highlightLine, detectLanguage } from '../utils/syntaxHighlight.js';
import { initDragResizer } from '../utils/resizer.js';

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

/**
 * Pairs up a sequential diff (equal/add/remove lines) into left/right rows so
 * the original and modified text can be rendered side by side, aligned like
 * a two-pane diff viewer: unchanged lines line up, and a run of removed lines
 * is paired against the run of added lines that replaced it. Each row from a
 * changed run is tagged with a `hunk` index (contiguous change group) so the
 * UI can jump between changes.
 */
function buildSideBySideRows(diffResult) {
    const rows = [];
    let i = 0;
    let hunkIndex = -1;

    while (i < diffResult.length) {
        const line = diffResult[i];

        if (line.type === 'equal') {
            rows.push({ left: line, right: line, hunk: null });
            i++;
            continue;
        }

        hunkIndex++;

        const removes = [];
        while (i < diffResult.length && diffResult[i].type === 'remove') {
            removes.push(diffResult[i]);
            i++;
        }

        const adds = [];
        while (i < diffResult.length && diffResult[i].type === 'add') {
            adds.push(diffResult[i]);
            i++;
        }

        const pairCount = Math.max(removes.length, adds.length);
        for (let j = 0; j < pairCount; j++) {
            rows.push({ left: removes[j] || null, right: adds[j] || null, hunk: hunkIndex });
        }
    }

    return rows;
}

function createDiffCell(line, lineNo, changeClass, language) {
    const cell = document.createElement('div');
    const lineNum = document.createElement('span');
    const text = document.createElement('span');

    lineNum.className = 'diff-line-num';
    text.className = 'diff-line-text';

    if (line) {
        cell.className = `diff-cell${changeClass ? ' ' + changeClass : ''}`;
        lineNum.textContent = lineNo;
        text.innerHTML = highlightLine(line.text, language);
    } else {
        cell.className = 'diff-cell diff-cell-empty';
        lineNum.textContent = '';
        text.textContent = ' ';
    }

    cell.appendChild(lineNum);
    cell.appendChild(text);

    return cell;
}

/**
 * Renders the side-by-side diff for a file and returns navigation info: the
 * first cell of each hunk (change group), for scrolling to it, plus +/- counts.
 */
function renderDiff(container, file) {
    container.innerHTML = '';

    const empty = { hunkElements: [], stats: { hunkCount: 0, additions: 0, deletions: 0 } };

    if (!file) return empty;

    const language = detectLanguage(file.filePath);
    const rows = buildSideBySideRows(diffLines(file.originalText, file.modifiedText));

    const fragment = document.createDocumentFragment();
    const hunkElements = [];
    let oldLineNo = 1;
    let newLineNo = 1;
    let additions = 0;
    let deletions = 0;

    for (const row of rows) {
        const leftClass = row.left ? (row.left.type === 'remove' ? 'diff-cell-remove' : '') : '';
        const rightClass = row.right ? (row.right.type === 'add' ? 'diff-cell-add' : '') : '';

        const leftCell = createDiffCell(row.left, row.left ? oldLineNo : null, leftClass, language);
        const rightCell = createDiffCell(row.right, row.right ? newLineNo : null, rightClass, language);

        if (row.hunk !== null && hunkElements[row.hunk] === undefined) {
            hunkElements[row.hunk] = leftCell;
        }

        fragment.appendChild(leftCell);
        fragment.appendChild(rightCell);

        if (row.left) oldLineNo++;
        if (row.right) newLineNo++;
        if (row.left && row.left.type === 'remove') deletions++;
        if (row.right && row.right.type === 'add') additions++;
    }

    container.appendChild(fragment);

    return {
        hunkElements,
        stats: { hunkCount: hunkElements.length, additions, deletions }
    };
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
    const diffChangeLabelEl = document.getElementById('diff-change-label');
    const diffDeletionsBadgeEl = document.getElementById('diff-deletions-badge');
    const diffAdditionsBadgeEl = document.getElementById('diff-additions-badge');
    const diffPrevBtn = document.getElementById('diff-prev-change');
    const diffNextBtn = document.getElementById('diff-next-change');
    const diffColumnsEl = document.getElementById('diff-columns');
    const diffColResizerEl = document.getElementById('diff-col-resizer');
    const sidebarResizerEl = document.getElementById('sidebar-resizer');
    const reviewSidebarEl = document.getElementById('review-sidebar');
    const acceptBtn = document.getElementById('accept-btn');
    const rejectBtn = document.getElementById('reject-btn');

    let currentChangeset = null;
    let selectedIndex = 0;
    let diffInfo = { hunkElements: [], stats: { hunkCount: 0, additions: 0, deletions: 0 } };
    let currentHunk = -1;

    function setBadge(el, sign, count) {
        el.textContent = `${sign}${count}`;
        el.classList.toggle('diff-badge-zero', count === 0);
    }

    function updateChangeNav() {
        const { hunkCount, additions, deletions } = diffInfo.stats;

        diffChangeLabelEl.textContent = `${hunkCount} change${hunkCount === 1 ? '' : 's'}`;
        setBadge(diffDeletionsBadgeEl, '-', deletions);
        setBadge(diffAdditionsBadgeEl, '+', additions);

        diffPrevBtn.disabled = hunkCount === 0;
        diffNextBtn.disabled = hunkCount === 0;
    }

    function goToHunk(index) {
        const count = diffInfo.stats.hunkCount;
        if (count === 0) return;

        diffInfo.hunkElements[currentHunk]?.classList.remove('diff-cell-current-hunk');

        currentHunk = ((index % count) + count) % count;

        const target = diffInfo.hunkElements[currentHunk];
        if (target) {
            target.classList.add('diff-cell-current-hunk');
            target.scrollIntoView({ block: 'center' });
        }
    }

    function selectFile(index) {
        selectedIndex = index;

        renderFileList(fileListEl, currentChangeset.files, selectedIndex, selectFile);

        const file = currentChangeset.files[selectedIndex];

        diffFilePathEl.textContent = file?.filePath || '';
        diffFileOperationEl.textContent = file?.operation || '';

        diffInfo = renderDiff(diffContentEl, file);
        currentHunk = -1;
        updateChangeNav();
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
    diffPrevBtn.addEventListener('click', () => goToHunk(currentHunk - 1));
    diffNextBtn.addEventListener('click', () => goToHunk(currentHunk + 1));

    // Drag the boundary between the Original/Modified columns.
    initDragResizer(diffColResizerEl, (e) => {
        const rect = diffColumnsEl.getBoundingClientRect();
        const pct = ((e.clientX - rect.left) / rect.width) * 100;

        diffColumnsEl.style.setProperty('--diff-split', Math.min(85, Math.max(15, pct)).toFixed(2));
    });
    diffColResizerEl.addEventListener('dblclick', () => {
        diffColumnsEl.style.setProperty('--diff-split', '50');
    });

    // Drag the boundary between the diff panel and the file list sidebar.
    initDragResizer(sidebarResizerEl, (e) => {
        const width = reviewSidebarEl.parentElement.getBoundingClientRect().right - e.clientX;

        reviewSidebarEl.style.width = `${Math.min(640, Math.max(180, width))}px`;
    });
    sidebarResizerEl.addEventListener('dblclick', () => {
        reviewSidebarEl.style.width = '300px';
    });

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
