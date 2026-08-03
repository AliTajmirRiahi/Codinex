/**
 * CodinexIcon.js
 * Custom element to render SVG icons from extension resources.
 * Usage: <codinex-icon name="send"></codinex-icon>
 */

class CodinexIcon extends HTMLElement {
    static get observedAttributes() {
        return ['name'];
    }

    async connectedCallback() {
        this.render();
    }

    attributeChangedCallback() {
        this.render();
    }

    async render() {
        const name = this.getAttribute('name');
        if (!name) return;

        try {
            // Using the custom protocol defined in the .NET side
            const url = `http://codinex.resources/Icons/${name}.svg`;
            const response = await fetch(url);

            if (response.ok) {
                const svgText = await response.text();
                this.innerHTML = svgText;
            } else {
                console.error(`[CodinexIcon] Failed to load icon: ${name}`);
            }
        } catch (err) {
            console.error(`[CodinexIcon] Error fetching icon: ${name}`, err);
        }
    }
}

// Register the component
if (!customElements.get('codinex-icon')) {
    customElements.define('codinex-icon', CodinexIcon);
}
