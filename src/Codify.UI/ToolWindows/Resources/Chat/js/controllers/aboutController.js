import { webViewTransport } from '../../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../constants/events.js';
import { $ } from '../utils/dom.js';

export function initAboutController() {
    const aboutButton = $('#about-menu-btn');
    const aboutModal = $('#about-modal');
    const closeButton = $('#close-about-modal');

    const openAboutModal = () => {
        aboutModal?.classList.remove('hidden');
    };

    const closeAboutModal = () => {
        aboutModal?.classList.add('hidden');
    };

    const openExternalLink = (url) => {
        if (!url)
            return;

        if (webViewTransport.isAvailable()) {
            webViewTransport.send(EVENTS.OPEN_EXTERNAL_LINK, { url });
            return;
        }

        window.open(url, '_blank', 'noopener,noreferrer');
    };

    aboutButton?.addEventListener('click', openAboutModal);
    closeButton?.addEventListener('click', closeAboutModal);

    aboutModal?.addEventListener('click', (event) => {
        if (event.target === aboutModal) {
            closeAboutModal();
            return;
        }

        const link = event.target.closest?.('.about-external-link');
        if (!link)
            return;

        event.preventDefault();
        openExternalLink(link.href);
    });
}
