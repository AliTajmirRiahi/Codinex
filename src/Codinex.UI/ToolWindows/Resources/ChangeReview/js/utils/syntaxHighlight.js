/**
 * syntaxHighlight.js
 * Small line-by-line syntax highlighter (comments/strings/numbers/keywords/
 * PascalCase-type heuristic). No external dependency — this diff view only
 * needs "reads like the language", not full-grammar correctness, and each
 * line is highlighted independently since that's the unit the diff renders.
 * Covers the languages Visual Studio project types commonly contain.
 */

const CLIKE_KEYWORDS = [
    'if', 'else', 'for', 'while', 'do', 'switch', 'case', 'default', 'break', 'continue',
    'return', 'try', 'catch', 'finally', 'throw', 'new', 'this', 'null', 'true', 'false',
    'static', 'const', 'void', 'class', 'struct', 'enum', 'public', 'private', 'protected'
];

const KEYWORD_SETS = {
    csharp: new Set([
        ...CLIKE_KEYWORDS,
        'internal', 'interface', 'readonly', 'string', 'bool', 'int', 'double', 'float',
        'decimal', 'long', 'short', 'byte', 'object', 'var', 'foreach', 'using', 'namespace',
        'base', 'async', 'await', 'get', 'set', 'partial', 'abstract', 'virtual', 'override',
        'sealed', 'event', 'delegate', 'out', 'ref', 'in', 'is', 'as', 'typeof', 'nameof',
        'record', 'init', 'yield', 'unsafe', 'fixed', 'checked', 'unchecked'
    ]),
    javascript: new Set([
        'const', 'let', 'var', 'function', 'return', 'if', 'else', 'for', 'while', 'do',
        'switch', 'case', 'default', 'break', 'continue', 'try', 'catch', 'finally', 'throw',
        'new', 'class', 'extends', 'this', 'super', 'import', 'export', 'from', 'as', 'async',
        'await', 'typeof', 'instanceof', 'in', 'of', 'null', 'undefined', 'true', 'false',
        'void', 'yield', 'static', 'get', 'set', 'interface', 'type', 'implements', 'public',
        'private', 'protected', 'readonly', 'enum', 'delete'
    ]),
    cpp: new Set([
        ...CLIKE_KEYWORDS,
        'int', 'char', 'bool', 'float', 'double', 'long', 'short', 'unsigned', 'signed',
        'auto', 'namespace', 'using', 'template', 'typename', 'virtual', 'override', 'friend',
        'inline', 'constexpr', 'nullptr', 'sizeof', 'union', 'typedef', 'volatile', 'extern',
        'goto', 'operator', 'explicit', 'delete', 'new', 'include', 'define', 'ifdef', 'endif'
    ]),
    vb: new Set([
        'public', 'private', 'protected', 'friend', 'shared', 'static', 'dim', 'as', 'new',
        'class', 'module', 'structure', 'interface', 'enum', 'sub', 'function', 'end',
        'if', 'then', 'else', 'elseif', 'for', 'each', 'to', 'step', 'next', 'while', 'do',
        'loop', 'until', 'select', 'case', 'try', 'catch', 'finally', 'throw', 'return',
        'imports', 'namespace', 'me', 'mybase', 'nothing', 'true', 'false', 'and', 'or',
        'not', 'is', 'in', 'of', 'byval', 'byref', 'optional', 'overrides', 'overridable',
        'readonly', 'const', 'string', 'integer', 'boolean', 'double', 'object', 'async',
        'await', 'property', 'get', 'set', 'implements', 'inherits', 'with', 'exit'
    ]),
    fsharp: new Set([
        'let', 'mutable', 'in', 'if', 'then', 'else', 'elif', 'match', 'with', 'for', 'to',
        'do', 'while', 'try', 'finally', 'with', 'exception', 'raise', 'type', 'module',
        'namespace', 'open', 'rec', 'and', 'fun', 'function', 'true', 'false', 'null',
        'member', 'static', 'private', 'public', 'internal', 'abstract', 'override', 'new',
        'of', 'as', 'when', 'yield', 'async', 'return', 'begin', 'end'
    ]),
    python: new Set([
        'def', 'class', 'return', 'if', 'elif', 'else', 'for', 'while', 'break', 'continue',
        'pass', 'try', 'except', 'finally', 'raise', 'with', 'as', 'import', 'from', 'global',
        'nonlocal', 'lambda', 'yield', 'async', 'await', 'True', 'False', 'None', 'and', 'or',
        'not', 'in', 'is', 'self', 'assert', 'del', 'print'
    ]),
    sql: new Set([
        'select', 'from', 'where', 'join', 'inner', 'left', 'right', 'outer', 'on', 'as',
        'insert', 'into', 'values', 'update', 'set', 'delete', 'create', 'table', 'alter',
        'drop', 'index', 'view', 'procedure', 'function', 'trigger', 'primary', 'key',
        'foreign', 'references', 'not', 'null', 'default', 'unique', 'check', 'constraint',
        'and', 'or', 'in', 'exists', 'between', 'like', 'order', 'by', 'group', 'having',
        'union', 'all', 'distinct', 'case', 'when', 'then', 'else', 'end', 'begin', 'commit',
        'rollback', 'transaction', 'declare', 'exec', 'execute', 'true', 'false'
    ]),
    powershell: new Set([
        'function', 'param', 'if', 'elseif', 'else', 'switch', 'foreach', 'for', 'while', 'do',
        'until', 'break', 'continue', 'return', 'try', 'catch', 'finally', 'throw', 'begin',
        'process', 'end', 'class', 'enum', 'true', 'false', 'null', 'and', 'or', 'not',
        'in', 'import', 'module', 'new', 'object'
    ]),
    yaml: new Set(['true', 'false', 'null']),
    xml: new Set([]),
    json: new Set(['true', 'false', 'null']),
    css: new Set(['important', 'inherit', 'initial', 'auto', 'none', 'from', 'to'])
};

KEYWORD_SETS.typescript = KEYWORD_SETS.javascript;
KEYWORD_SETS.c = KEYWORD_SETS.cpp;
KEYWORD_SETS.markup = KEYWORD_SETS.xml;
KEYWORD_SETS.html = KEYWORD_SETS.xml;
KEYWORD_SETS.scss = KEYWORD_SETS.css;
KEYWORD_SETS.less = KEYWORD_SETS.css;

// Languages whose keywords are conventionally written in any casing
// (SELECT/select, Dim/DIM, ForEach-Object/foreach-object, ...).
const CASE_INSENSITIVE_LANGS = new Set(['sql', 'vb', 'powershell']);

const TOKEN_REGEX =
    /(\/\/.*$)|(\/\*[\s\S]*?\*\/)|(<!--[\s\S]*?-->)|(--.*$)|(#.*$)|("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|`(?:\\.|[^`\\])*`)|(\b0x[0-9a-fA-F]+\b|\b\d+\.?\d*(?:[eE][+-]?\d+)?[a-zA-Z]{0,2}\b)|([A-Za-z_$][A-Za-z0-9_$]*)/g;

const HTML_ESCAPES = { '&': '&amp;', '<': '&lt;', '>': '&gt;' };

function escapeHtml(text) {
    return text.replace(/[&<>]/g, ch => HTML_ESCAPES[ch]);
}

/**
 * Highlights a single line of source code, returning safe HTML (the caller
 * assigns it to innerHTML directly — all raw text is escaped internally).
 */
export function highlightLine(text, language) {
    if (!text) return '';

    const keywordSet = KEYWORD_SETS[language];
    if (!keywordSet) return escapeHtml(text);

    const caseInsensitive = CASE_INSENSITIVE_LANGS.has(language);

    let out = '';
    let lastIndex = 0;
    let match;

    TOKEN_REGEX.lastIndex = 0;

    while ((match = TOKEN_REGEX.exec(text))) {
        out += escapeHtml(text.slice(lastIndex, match.index));

        const [full, lineComment, blockComment, xmlComment, dashComment, hashComment, str, num, word] = match;

        if (lineComment || blockComment || xmlComment || dashComment || hashComment) {
            out += `<span class="tok-comment">${escapeHtml(full)}</span>`;
        } else if (str) {
            out += `<span class="tok-string">${escapeHtml(full)}</span>`;
        } else if (num) {
            out += `<span class="tok-number">${escapeHtml(full)}</span>`;
        } else if (word) {
            if (keywordSet.has(caseInsensitive ? word.toLowerCase() : word)) {
                out += `<span class="tok-keyword">${escapeHtml(full)}</span>`;
            } else if (/^[A-Z]/.test(word)) {
                out += `<span class="tok-type">${escapeHtml(full)}</span>`;
            } else {
                out += escapeHtml(full);
            }
        } else {
            out += escapeHtml(full);
        }

        lastIndex = match.index + full.length;
    }

    out += escapeHtml(text.slice(lastIndex));

    return out;
}

const EXTENSION_LANGUAGE = {
    // C#
    cs: 'csharp',
    csx: 'csharp',
    // JavaScript / TypeScript
    js: 'javascript',
    mjs: 'javascript',
    cjs: 'javascript',
    jsx: 'javascript',
    ts: 'javascript',
    tsx: 'javascript',
    // C / C++
    c: 'cpp',
    h: 'cpp',
    cpp: 'cpp',
    cxx: 'cpp',
    cc: 'cpp',
    hpp: 'cpp',
    hxx: 'cpp',
    // Visual Basic .NET
    vb: 'vb',
    // F#
    fs: 'fsharp',
    fsx: 'fsharp',
    fsi: 'fsharp',
    // Python
    py: 'python',
    pyw: 'python',
    // SQL
    sql: 'sql',
    // PowerShell
    ps1: 'powershell',
    psm1: 'powershell',
    psd1: 'powershell',
    // Data / markup
    json: 'json',
    yml: 'yaml',
    yaml: 'yaml',
    xml: 'xml',
    xaml: 'xml',
    csproj: 'xml',
    vbproj: 'xml',
    fsproj: 'xml',
    config: 'xml',
    resx: 'xml',
    nuspec: 'xml',
    targets: 'xml',
    props: 'xml',
    html: 'html',
    htm: 'html',
    cshtml: 'html',
    vbhtml: 'html',
    // Styles
    css: 'css',
    scss: 'scss',
    less: 'less'
};

/** Maps a file path's extension to a highlighter language id, or null if unsupported. */
export function detectLanguage(filePath) {
    const match = /\.([a-zA-Z0-9]+)$/.exec(filePath || '');

    if (!match) return null;

    return EXTENSION_LANGUAGE[match[1].toLowerCase()] || null;
}
