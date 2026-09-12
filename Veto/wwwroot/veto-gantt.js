// Interop for VetoGantt: file downloads and horizontal scrolling.

/**
 * Downloads text as a file.
 *
 * Uses an object URL rather than a data: URL because a data URL of a large export can exceed
 * what a browser will accept in an href, and revokes it afterwards so the blob is not held for
 * the life of the page.
 */
export function download(fileName, mimeType, content) {
    const blob = new Blob([content], { type: `${mimeType};charset=utf-8` });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();

    URL.revokeObjectURL(url);
}

/** Scrolls the timeline so a given pixel offset sits a little in from the left edge. */
export function scrollTo(element, offset) {
    if (!element) {
        return;
    }

    element.scrollTo({ left: Math.max(0, offset - element.clientWidth / 3), behavior: 'smooth' });
}

/** Scrolls the timeline by a fraction of its visible width. */
export function scrollBy(element, fraction) {
    if (!element) {
        return;
    }

    element.scrollBy({ left: element.clientWidth * fraction, behavior: 'smooth' });
}
