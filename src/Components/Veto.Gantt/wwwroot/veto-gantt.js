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

/**
 * Scrolls the timeline so a given pixel offset sits a fraction of the way in from the left: the
 * centre for the now marker, a third for a date, with what comes after it in view. Smooth when
 * the reader asked for it; instant when the chart is placing itself, where an animation would
 * only be a delay.
 */
export function scrollTo(element, offset, smooth = true, at = 1 / 3) {
    if (!element) {
        return;
    }

    element.scrollTo({ left: Math.max(0, offset - element.clientWidth * at), behavior: smooth ? 'smooth' : 'instant' });
}

/**
 * Scrolls the timeline by a fraction of its visible width. Returns false when it is already as
 * far as it goes that way - a range fitted to the width never scrolls at all - so the chart can
 * page the range instead of doing nothing.
 */
export function scrollBy(element, fraction) {
    if (!element) {
        return false;
    }

    const max = element.scrollWidth - element.clientWidth;
    const atEnd = fraction > 0 ? element.scrollLeft >= max - 1 : element.scrollLeft <= 1;

    if (atEnd) {
        return false;
    }

    element.scrollBy({ left: element.clientWidth * fraction, behavior: 'smooth' });
    return true;
}

/** Jumps the timeline to its start or its end, as a paged range is redrawn. */
export function scrollToEdge(element, end) {
    if (!element) {
        return;
    }

    element.scrollTo({ left: end ? element.scrollWidth : 0, behavior: 'instant' });
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

// The listeners keeping each open card against its bar, so a new placement replaces them.
const tooltipTrackers = new WeakMap();

/**
 * Places a hover card against the bar it describes, in window coordinates: below the bar, or
 * above it when there is no room below, and kept inside the window. It follows the bar while
 * anything scrolls, and hides while the bar is scrolled out of the timeline's view.
 */
export function placeTooltip(card, root, itemId) {
    if (!card || !root) {
        return;
    }

    tooltipTrackers.get(card)?.();

    const anchor = root.querySelector(`[data-item-id="${CSS.escape(itemId)}"]:not([role="tooltip"])`);
    const scroller = root.querySelector('.veto-gantt__scroller');

    const stop = () => {
        window.removeEventListener('scroll', place, true);
        window.removeEventListener('resize', place);
        tooltipTrackers.delete(card);
    };

    function place() {
        if (!card.isConnected || !anchor || !anchor.isConnected) {
            stop();
            return;
        }

        const bar = anchor.getBoundingClientRect();
        const view = scroller ? scroller.getBoundingClientRect() : bar;

        // A bar scrolled out of the timeline has nothing on screen to point at.
        if (bar.right < view.left || bar.left > view.right || bar.bottom < view.top || bar.top > view.bottom) {
            card.style.visibility = 'hidden';
            return;
        }

        const gap = 6;
        const margin = 8;
        const box = card.getBoundingClientRect();

        const fitsBelow = bar.bottom + gap + box.height <= window.innerHeight - margin;
        const fitsAbove = bar.top - gap - box.height >= margin;
        const top = fitsBelow || !fitsAbove ? bar.bottom + gap : bar.top - gap - box.height;

        const pointer = Math.max(bar.left, view.left);
        const left = Math.max(margin, Math.min(pointer, window.innerWidth - box.width - margin));

        card.style.left = `${left}px`;
        card.style.top = `${top}px`;

        // A transformed ancestor - a tilted card, an extruded panel - becomes a fixed element's
        // frame of reference, so measure where it landed and correct by the difference.
        const landed = card.getBoundingClientRect();
        card.style.left = `${left + (left - landed.left)}px`;
        card.style.top = `${top + (top - landed.top)}px`;

        card.style.visibility = 'visible';
    }

    window.addEventListener('scroll', place, true);
    window.addEventListener('resize', place);
    tooltipTrackers.set(card, stop);

    place();
}
