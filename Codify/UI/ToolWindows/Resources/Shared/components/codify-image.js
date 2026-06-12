/**
 * CodifyImage.js
 * Custom element to render images from extension resources.
 * Usage: <codify-image name="avatar.png"></codify-image>
 */

class CodifyImage extends HTMLElement {

    async connectedCallback() {

        const name = this.getAttribute("name");

        if (!name)
            return;

        const url = `http://codify.resources/Icons/${name}`;

        try {

            const response = await fetch(url);
            const img = await response.text();

            this.innerHTML = img;

        } catch (err) {

            console.error("Image load failed:", name, err);
        }
    }
}

if (!customElements.get('codify-image')) {
    customElements.define('codify-image', CodifyImage);
}
