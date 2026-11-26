// Scrolling helpers for VetoTable.
//
// observeEnd: tells the component when the end of the table comes into view, for infinite
// scroll. An IntersectionObserver rather than a scroll listener: it costs nothing while the user
// scrolls, and it already accounts for any scrolling ancestor - the table's own wrapper with
// RootMaxHeight, or the page itself - clipping the sentinel out of sight.

/**
 * Scrolls the table's own box back to its first row - after a page change or a new stream.
 * Only the box: the page itself stays put, so the button just pressed stays under the pointer.
 * @param {Element} element the table's root, which is its scroll container
 */
export function scrollToTop(element) {
    if (element) {
        element.scrollTop = 0;
    }
}

/**
 * Watches the sentinel after the last row and calls back into .NET when it becomes visible.
 * @param {Element} sentinel the element just after the last row
 * @param {object} component the .NET reference to call
 * @returns {{ recheck: () => void, dispose: () => void }}
 */
export function observeEnd(sentinel, component) {
    let busy = false;

    const observer = new IntersectionObserver(async entries => {
        if (busy || !entries.some(entry => entry.isIntersecting)) {
            return;
        }

        busy = true;
        try {
            await component.invokeMethodAsync('OnSentinelVisibleAsync');
        } catch {
            // The component has gone away; the next dispose call tidies up.
        } finally {
            busy = false;
        }
    }, {
        // Start the next batch a little before the end is actually reached.
        rootMargin: '0px 0px 200px 0px'
    });

    observer.observe(sentinel);

    return {
        // An observer only reports changes. After a batch that did not fill the view the end is
        // still visible, so nothing would fire again: observing afresh reports it once more.
        recheck() {
            observer.unobserve(sentinel);
            observer.observe(sentinel);
        },
        dispose() {
            observer.disconnect();
        }
    };
}