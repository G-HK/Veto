// Scroll-spy for VetoHandbookToc.
//
// Works out which section the reader is in and tells .NET only when that changes - never per
// scroll event. Listens on window in the capture phase, because scroll does not bubble: that
// way it hears the window and any scrolling container the page happens to live in.

const handles = new WeakMap();

/**
 * The top of the box a section scrolls in: 0 for the window, or the top edge of a scrolling
 * container - a layout's main area, say - which is where "the top" is for a reader in there.
 */
function scrollTop(element) {
    for (let parent = element.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
        const overflow = getComputedStyle(parent).overflowY;

        if ((overflow === 'auto' || overflow === 'scroll') && parent.scrollHeight > parent.clientHeight) {
            return parent.getBoundingClientRect().top;
        }
    }

    return 0;
}

/**
 * Starts tracking the current section.
 * @param {HTMLElement} nav the contents rail, used as the handle's key
 * @param {any} dotnet the component's DotNetObjectReference
 * @param {string[]} ids the section ids, in page order
 * @param {number} offset px from the top of the viewport that counts as "reading here"
 */
export function observe(nav, dotnet, ids, offset) {
    unobserve(nav);

    let current;
    let frame = 0;

    const pick = () => {
        frame = 0;

        // The last section whose top has passed the reading line. Looked up each time rather
        // than cached, so sections rendered after the rail are still found.
        let active = null;

        for (const id of ids) {
            const element = document.getElementById(id);

            // A section's own scroll-margin-top is where an anchor jump leaves it, so that
            // line counts as reached too - otherwise jumping to a section would not mark it.
            const margin = element ? parseFloat(getComputedStyle(element).scrollMarginTop) || 0 : 0;

            if (element && element.getBoundingClientRect().top - scrollTop(element) - Math.max(offset, margin) <= 1) {
                active = id;
            }
        }

        if (active !== current) {
            current = active;
            dotnet.invokeMethodAsync('OnSpiedSection', active);
        }
    };

    const onScroll = () => {
        if (!frame) {
            frame = requestAnimationFrame(pick);
        }
    };

    window.addEventListener('scroll', onScroll, { capture: true, passive: true });
    window.addEventListener('resize', onScroll, { passive: true });
    handles.set(nav, () => {
        cancelAnimationFrame(frame);
        window.removeEventListener('scroll', onScroll, { capture: true });
        window.removeEventListener('resize', onScroll);
    });

    pick();
}

/** Stops tracking. */
export function unobserve(nav) {
    const stop = nav && handles.get(nav);

    if (stop) {
        stop();
        handles.delete(nav);
    }
}
