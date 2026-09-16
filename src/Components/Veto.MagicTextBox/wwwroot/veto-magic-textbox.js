// Overflow measurement, auto-grow and clipboard interop for VetoMagicTextBox.

const observers = new WeakMap();

/**
 * The height the field occupies when closed, in pixels, derived from the same line count the
 * stylesheet uses. Measured rather than read back off the element so the answer is the same
 * whether the field is currently open or closed - otherwise opening the field would report
 * "fits" and take away the control that closes it again.
 */
function collapsedHeight(textarea, collapsedLines) {
    const style = getComputedStyle(textarea);

    // A line-height of "normal" computes to a keyword rather than a length; 1.5em is the
    // usual browser interpretation and close enough to keep the toggle honest.
    const lineHeight = parseFloat(style.lineHeight) || parseFloat(style.fontSize) * 1.5;
    const padding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom);

    return lineHeight * collapsedLines + padding;
}

/** True when the value needs more room than the closed field gives it. */
function overflows(textarea, collapsedLines) {
    // scrollHeight includes padding but not border, so compare it against a collapsed height
    // built the same way. A pixel of slack absorbs sub-pixel rounding, which otherwise reports
    // an overflow on a value that fits exactly.
    return textarea.scrollHeight - collapsedHeight(textarea, collapsedLines) > 1;
}

/**
 * Applies the open height. The stylesheet owns the closed height and the fixed-line open
 * height; only "open as far as the text needs" has to be measured, because no CSS length
 * describes the height of a textarea's content.
 */
function applyHeight(textarea, expanded, expandedLines) {
    if (expanded && !expandedLines) {
        textarea.style.height = 'auto';
        textarea.style.height = `${textarea.scrollHeight}px`;
    } else {
        textarea.style.height = '';
    }
}

/**
 * Starts watching the field and returns whether it currently overflows. The observer catches
 * the field being resized and the user typing; a value replaced from code is picked up by the
 * component calling sync on its next render.
 */
export function attach(textarea, dotNetRef, collapsedLines) {
    if (!textarea) {
        return false;
    }

    detach(textarea);

    let last = overflows(textarea, collapsedLines);

    const report = () => {
        const now = overflows(textarea, collapsedLines);

        if (now !== last) {
            last = now;
            dotNetRef.invokeMethodAsync('OnOverflowChangedAsync', now);
        }
    };

    const observer = new ResizeObserver(report);
    observer.observe(textarea);
    textarea.addEventListener('input', report);

    observers.set(textarea, () => {
        observer.disconnect();
        textarea.removeEventListener('input', report);
    });

    return last;
}

/** Re-measures and re-applies the open height. Called on every render after the first. */
export function sync(textarea, collapsedLines, expanded, expandedLines) {
    if (!textarea) {
        return false;
    }

    applyHeight(textarea, expanded, expandedLines);

    return overflows(textarea, collapsedLines);
}

/** Stops watching a field whose component is going away. */
export function detach(textarea) {
    const dispose = textarea && observers.get(textarea);

    if (dispose) {
        dispose();
        observers.delete(textarea);
    }
}

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
