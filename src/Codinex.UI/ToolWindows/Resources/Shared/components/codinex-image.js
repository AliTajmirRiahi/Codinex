/**
 * CodinexImage.js
 * Custom element to render images from extension resources.
 * Usage: <codinex-image name="avatar.png"></codinex-image>
 */

class CodinexImage extends HTMLElement {

    async connectedCallback() {

        const name = this.getAttribute("name");

        if (!name)
            return;

        const url = `http://codinex.resources/Icons/${name}`;

        try {

            const response = await fetch(url);
            const img = await response.text();

            this.innerHTML = img;

        } catch (err) {

            console.error("Image load failed:", name, err);
        }
    }
}

if (!customElements.get('codinex-image')) {
    customElements.define('codinex-image', CodinexImage);
}
