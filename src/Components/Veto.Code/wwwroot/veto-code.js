// Clipboard interop for VetoCode.

/**
 * Copies text to the clipboard. Returns false rather than throwing when the browser refuses -
 * the Clipboard API needs a secure context and a user gesture, and neither is guaranteed.
 */
export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}

/** Copies an element's rendered text - for code supplied as markup rather than a string. */
export function copyElementText(element) {
    return copyText(element?.textContent ?? "");
}
