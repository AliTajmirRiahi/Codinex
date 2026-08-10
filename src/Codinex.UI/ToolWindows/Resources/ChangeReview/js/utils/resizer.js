/**
 * resizer.js
 * Generic pointer-drag helper for resizer bars (sidebar width, diff column
 * split). The caller supplies the pixel/percentage math via onDrag.
 */
export function initDragResizer(handleEl, onDrag) {
    function handlePointerMove(e) {
        onDrag(e);
    }

    function handlePointerUp() {
        document.removeEventListener('pointermove', handlePointerMove);
        document.removeEventListener('pointerup', handlePointerUp);
        handleEl.classList.remove('resizing');
        document.body.classList.remove('resizing-col');
    }

    handleEl.addEventListener('pointerdown', (e) => {
        e.preventDefault();
        handleEl.classList.add('resizing');
        document.body.classList.add('resizing-col');
        document.addEventListener('pointermove', handlePointerMove);
        document.addEventListener('pointerup', handlePointerUp);
    });
}
