export class CodeRenderer {

    /**
     * Process AI text and convert
    code blocks
    into styled HTML boxes
     */
    static render(text) {

        if (!text) return "";

        text = this.normalizeText(text);

        text = this.escapeHtml(text);

        const codeRegex = /```(\w*)\n?([\s\S]*?)```/g;

        text = text.replace(codeRegex, (match, lang, code) => {

            const language = lang || "code";
            const escapedCode = code.trim();
            const uniqueId = "code-" + Math.random().toString(36).substring(2, 10);

            return `
                <div class="code-wrapper">
                    <div class="code-header">
                        <span class="code-lang">${language}</span>
                        <button class="copy-btn" data-code="${uniqueId}">
                            Copy
                        </button>
                    </div>
                    <pre><code id="${uniqueId}" class="language-${language}">${escapedCode}</code></pre>
                </div>
                `;
        });

        return text;
    }

    /**
     * Prevent XSS by escaping HTML
     */
    static escapeHtml(text) {

        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    /**
     * Attach copy button listeners after render
     */
    static bindCopyEvents(container) {

        const buttons = container.querySelectorAll(".copy-btn");

        buttons.forEach(btn => {

            btn.addEventListener("click", () => {

                const id = btn.dataset.code;
                const code = document.getElementById(id)?.textContent;

                if (!code) return;

                navigator.clipboard.writeText(code);

                const original = btn.textContent;
                btn.textContent = "Copied";

                setTimeout(() => {
                    btn.textContent = original;
                }, 1500);

            });

        });
    }
    static normalizeText(text) {
        return text
            .replace(/\r\n/g, "\n")               // Normalize Windows line endings
            .replace(/[ \t]+\n/g, "\n")           // Remove trailing spaces before newline
            .replace(/\n\s*\n\s*\n+/g, "\n\n")    // Collapse 3+ blank lines into 2
            .replace(/[ \t]{2,}/g, " ")           // Collapse multiple spaces
            .trim();
    }
    //static normalizeHtml(html) {
    //    const div = document.createElement("div");
    //    div.innerHTML = html;

    //    const walker = document.createTreeWalker(div, NodeFilter.SHOW_TEXT);

    //    let node;
    //    while (node = walker.nextNode()) {
    //        node.nodeValue = node.nodeValue
    //            .replace(/\s+/g, " ")
    //            .trim();
    //    }

    //    return div.innerHTML;
    //}

}