// Clipboard interop for VetoTerminalBox.

/**
 * Gathers the commands in a terminal window, one per line, skipping prompts and output.
 *
 * Read from the DOM rather than assembled from the component tree on purpose: a line that
 * registered itself with its window would have to compare its parameters before notifying it,
 * or the two would re-render each other without end. The rendered text is also the honest
 * answer - it is what the reader is looking at, whatever produced it.
 */
export function collectCommands(root) {
    if (!root) {
        return '';
    }

    return Array.from(root.querySelectorAll('[data-terminal-command]'))
        .map(el => el.textContent.trim())
        .filter(Boolean)
        .join('\n');
}

/**
 * Copies text to the clipboard. Returns false rather than throwing when the browser refuses -
 * the Clipboard API needs a secure context and a user gesture, and neither is guaranteed.
 */
export async function copyText(text) {
    try
    {
        await navigator.clipboard.writeText(text);
        return true;
    }
    catch
    {
        return false;
    }
}