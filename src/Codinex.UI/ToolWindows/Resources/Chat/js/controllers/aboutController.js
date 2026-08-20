import { webViewTransport } from '../../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../constants/events.js';
import { $ } from '../utils/dom.js';
import { CodeRenderer } from '../../../Shared/components/code-renderer.js';

export function initAboutController() {
    const aboutButton = $('#about-menu-btn');
    const aboutModal = $('#about-modal');
    const closeButton = $('#close-about-modal');

    const donateButton = $('#open-donate-modal');
    const donateModal = $('#donate-modal');
    const closeDonateButton = $('#close-donate-modal');

    const openAboutModal = () => {
        aboutModal?.classList.remove('hidden');
    };

    const closeAboutModal = () => {
        aboutModal?.classList.add('hidden');
    };

    const openDonateModal = () => {
        closeAboutModal();
        donateModal?.classList.remove('hidden');
    };

    const closeDonateModal = () => {
        donateModal?.classList.add('hidden');
    };

    const copyDonateAddress = async (buttonEl) => {
        const address = buttonEl.dataset.address;
        if (!address)
            return;

        const originalTitle = buttonEl.title;
        const originalLabel = buttonEl.getAttribute('aria-label');

        try {
            await CodeRenderer.copyToClipboard(address);

            buttonEl.title = 'Copied';
            buttonEl.setAttribute('aria-label', 'Copied');
            buttonEl.classList.add('copied');
        } catch {
            buttonEl.title = 'Copy failed';
            buttonEl.setAttribute('aria-label', 'Copy failed');
        }

        setTimeout(() => {
            buttonEl.title = originalTitle;

            if (originalLabel) {
                buttonEl.setAttribute('aria-label', originalLabel);
            } else {
                buttonEl.removeAttribute('aria-label');
            }

            buttonEl.classList.remove('copied');
        }, 1500);
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

    donateButton?.addEventListener('click', openDonateModal);
    closeDonateButton?.addEventListener('click', closeDonateModal);

    donateModal?.addEventListener('click', (event) => {
        if (event.target === donateModal) {
            closeDonateModal();
            return;
        }

        const copyButton = event.target.closest?.('.donate-copy-btn');
        if (!copyButton)
            return;

        copyDonateAddress(copyButton);
    });
}
