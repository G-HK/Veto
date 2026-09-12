// Layout interop. Deliberately tiny: the shell's responsive behaviour is CSS, so the only
// thing JavaScript is needed for is stopping the page behind an overlay drawer from scrolling.

let lockCount = 0;
let previousOverflow = '';

/**
 * Locks or unlocks page scrolling. Reference-counted, so two overlays open at once do not
 * leave scrolling broken when only one of them closes.
 */
export function setScrollLock(locked) {
    const body = document.body;

    if (locked) {
        if (lockCount === 0) {
            previousOverflow = body.style.overflow;
            body.style.overflow = 'hidden';
        }

        lockCount++;
        return;
    }

    lockCount = Math.max(0, lockCount - 1);

    if (lockCount === 0) {
        body.style.overflow = previousOverflow;
    }
}

/**
 * Whether the viewport is at least `minWidth` CSS pixels wide.
 *
 * Called once, when a responsive drawer first renders, so it can decide whether to start open.
 * A drawer that defaults to open would otherwise cover the whole screen on a phone the moment
 * the page loads. This is a single call at startup, not a resize listener - everything after it
 * is handled by the media queries in the drawer's stylesheet.
 */
export function matchesMinWidth(minWidth) {
    return window.matchMedia(`(min-width: ${minWidth}px)`).matches;
}
