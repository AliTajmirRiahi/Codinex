/**
 * numberStepper.js
 * Replaces the native OS-drawn spin buttons on <input type="number"> with a pair of
 * small themed buttons (styled in _inputs.css), so numeric settings match the rest
 * of the UI instead of looking like a stock Windows control.
 */
export function enhanceNumberInputs(root = document) {
    const inputs = root.querySelectorAll('input[type="number"]:not([data-stepper-enhanced])');

    inputs.forEach((input) => {
        input.dataset.stepperEnhanced = 'true';

        const wrapper = document.createElement('div');
        wrapper.className = 'number-stepper';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        const controls = document.createElement('div');
        controls.className = 'number-stepper-controls';

        const upButton = document.createElement('button');
        upButton.type = 'button';
        upButton.setAttribute('aria-label', 'Increase');
        upButton.tabIndex = -1;
        upButton.textContent = '▲';

        const downButton = document.createElement('button');
        downButton.type = 'button';
        downButton.setAttribute('aria-label', 'Decrease');
        downButton.tabIndex = -1;
        downButton.textContent = '▼';

        const step = (direction) => {
            if (input.disabled) return;

            if (direction > 0) input.stepUp();
            else input.stepDown();

            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        };

        upButton.addEventListener('click', () => step(1));
        downButton.addEventListener('click', () => step(-1));

        controls.appendChild(upButton);
        controls.appendChild(downButton);
        wrapper.appendChild(controls);
    });
}
