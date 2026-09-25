// Where focus was when the menu opened, so closing it can put focus back.
let returnTo = null;

// Pulls the panel back inside the window if the pointer was near an edge, then focuses its first item.
export function openMenu(panel) {
    if (!panel) {
        return;
    }

    returnTo = document.activeElement;

    const box = panel.getBoundingClientRect();
    if (box.right > window.innerWidth) {
        panel.style.left = `${Math.max(0, window.innerWidth - box.width - 4)}px`;
    }
    if (box.bottom > window.innerHeight) {
        panel.style.top = `${Math.max(0, window.innerHeight - box.height - 4)}px`;
    }

    moveMenuFocus(panel, 1);
}

// Moves focus to the next or previous enabled item in the menu, wrapping at either end.
export function moveMenuFocus(panel, step) {
    const items = [...(panel?.querySelectorAll('[role^="menuitem"]:not([disabled])') ?? [])];
    if (items.length === 0) {
        panel?.focus();
        return;
    }

    const at = items.indexOf(document.activeElement);
    const next = at < 0 ? (step > 0 ? 0 : items.length - 1) : (at + step + items.length) % items.length;
    items[next].focus();
}

export function restoreFocus() {
    if (returnTo?.isConnected && returnTo !== document.body) {
        returnTo.focus();
    }
    returnTo = null;
}