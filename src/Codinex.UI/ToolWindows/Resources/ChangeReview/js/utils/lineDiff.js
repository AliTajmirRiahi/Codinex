/**
 * lineDiff.js
 * Small LCS-based line diff. No external dependency (jsdiff was considered but a
 * ~40 line in-house implementation covers this view's needs without vendoring
 * a third-party library).
 */

const MAX_CELLS = 4_000_000;

export function diffLines(oldText, newText) {
    const a = (oldText ?? '').split('\n');
    const b = (newText ?? '').split('\n');

    if (a.length * b.length > MAX_CELLS) {
        return [
            ...a.map(text => ({ type: 'remove', text })),
            ...b.map(text => ({ type: 'add', text }))
        ];
    }

    const n = a.length;
    const m = b.length;

    const dp = new Array(n + 1);
    for (let i = 0; i <= n; i++) dp[i] = new Uint32Array(m + 1);

    for (let i = n - 1; i >= 0; i--) {
        for (let j = m - 1; j >= 0; j--) {
            dp[i][j] = a[i] === b[j]
                ? dp[i + 1][j + 1] + 1
                : Math.max(dp[i + 1][j], dp[i][j + 1]);
        }
    }

    const result = [];
    let i = 0;
    let j = 0;

    while (i < n && j < m) {
        if (a[i] === b[j]) {
            result.push({ type: 'equal', text: a[i] });
            i++;
            j++;
        } else if (dp[i + 1][j] >= dp[i][j + 1]) {
            result.push({ type: 'remove', text: a[i] });
            i++;
        } else {
            result.push({ type: 'add', text: b[j] });
            j++;
        }
    }

    while (i < n) result.push({ type: 'remove', text: a[i++] });
    while (j < m) result.push({ type: 'add', text: b[j++] });

    return result;
}
