// Interop for VetoGantt: file downloads, horizontal scrolling and width tracking.

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

// One observer per timeline, kept on the element so a re-observe replaces rather than stacks.
const widthObservers = new WeakMap();

/**
 * Reports the timeline's visible width now and whenever it changes, so the chart can stretch a
 * short axis to fill it. clientWidth rather than the observer's own measurement: it leaves out
 * a vertical scrollbar, which is exactly the room the axis must not spill into.
 */
export function observeWidth(element, dotNetRef) {
    if (!element) {
        return;
    }

    unobserveWidth(element);

    let last = -1;
    const report = () => {
        const width = Math.floor(element.clientWidth);

        if (width !== last) {
            last = width;
            dotNetRef.invokeMethodAsync('OnViewportResized', width).catch(() => { });
        }
    };

    const observer = new ResizeObserver(report);
    observer.observe(element);
    widthObservers.set(element, observer);
    report();
}

/** Stops reporting a timeline's width. */
export function unobserveWidth(element) {
    const observer = element && widthObservers.get(element);

    if (observer) {
        observer.disconnect();
        widthObservers.delete(element);
    }
}
