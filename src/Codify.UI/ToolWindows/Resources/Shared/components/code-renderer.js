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
                        <button class="copy-btn" data-code="${uniqueId}" title="Copy code" aria-label="Copy code">
                            <codify-icon name="symbols/copy" aria-hidden="true"></codify-icon>
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

            if (btn.dataset.copyBound === "true") return;

            btn.dataset.copyBound = "true";

            btn.addEventListener("click", async () => {

                const id = btn.dataset.code;
                const code = document.getElementById(id)?.textContent;

                if (!code) return;

                const originalTitle = btn.title;
                const originalLabel = btn.getAttribute("aria-label");

                try {
                    await CodeRenderer.copyToClipboard(code);

                    btn.title = "Copied";
                    btn.setAttribute("aria-label", "Copied");
                    btn.classList.add("copied");
                } catch {
                    btn.title = "Copy failed";
                    btn.setAttribute("aria-label", "Copy failed");
                }

                setTimeout(() => {
                    btn.title = originalTitle;

                    if (originalLabel) {
                        btn.setAttribute("aria-label", originalLabel);
                    } else {
                        btn.removeAttribute("aria-label");
                    }

                    btn.classList.remove("copied");
                }, 1500);

            });

        });
    }

    static copyToClipboard(text) {

        if (navigator.clipboard?.writeText) {
            return navigator.clipboard.writeText(text);
        }

        return new Promise((resolve, reject) => {
            const textArea = document.createElement("textarea");

            textArea.value = text;
            textArea.setAttribute("readonly", "");
            textArea.style.position = "fixed";
            textArea.style.left = "-9999px";
            textArea.style.top = "0";

            document.body.appendChild(textArea);
            textArea.select();

            try {
                const copied = document.execCommand("copy");

                document.body.removeChild(textArea);

                if (copied) {
                    resolve();
                } else {
                    reject(new Error("Copy command failed"));
                }
            } catch (error) {
                document.body.removeChild(textArea);
                reject(error);
            }
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