// Pointer tracking for VetoDock's magnified appearance.
//
// Magnification falls off across neighbouring items, which depends on the pointer's continuous
// position rather than on which element happens to be hovered - so it cannot be expressed in
// CSS alone. As with the tilt, the listeners live here and write custom properties; nothing
// crosses into .NET while the pointer moves.

const handles = new WeakMap();

function prefersReducedMotion() {
    return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Starts magnifying a dock's items as the pointer passes along it.
 * @param {HTMLElement} dock the dock element
 * @param {{magnification: number, range: number, vertical: boolean}} options
 */
export function attach(dock, initial) {
    if (!dock || handles.has(dock)) {
        return;
    }

    // Magnification is decoration that follows a hover position. Without a fine pointer there
    // is nothing to follow, and someone who asked for less motion should not get any of it.
    if (prefersReducedMotion() || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
        return;
    }

    // One options object per dock, changed in place by update(): the handler reads it on every
    // move. Captured in the closure as before, a dock moved from the bottom edge to a side kept
    // measuring along x, so every item in the column magnified at once.
    const options = { ...initial };
    const items = () => Array.from(dock.querySelectorAll('.veto-dock__item'));

    const onMove = event => {
        const pointer = options.vertical ? event.clientY : event.clientX;

        for (const item of items()) {
            const rect = item.getBoundingClientRect();
            const centre = options.vertical
                ? rect.top + rect.height / 2
                : rect.left + rect.width / 2;

            const distance = Math.abs(pointer - centre);

            // A raised cosine: 1 at the pointer, easing to 0 at the edge of the range. Linear
            // falloff makes the neighbours look like they snap; this reads as a wave.
            const influence = distance >= options.range
                ? 0
                : (Math.cos((distance / options.range) * Math.PI) + 1) / 2;

            item.style.setProperty('--veto-dock-scale', String(1 + (options.magnification - 1) * influence));
        }
    };

    const onLeave = () => {
        for (const item of items()) {
            item.style.setProperty('--veto-dock-scale', '1');
        }
    };

    dock.addEventListener('pointermove', onMove);
    dock.addEventListener('pointerleave', onLeave);

    handles.set(dock, { onMove, onLeave, options });
}

/** Applies changed options to an attached dock without re-attaching it. */
export function update(dock, changed) {
    const handle = dock && handles.get(dock);

    if (handle) {
        Object.assign(handle.options, changed);
        handle.onLeave();
    }
}

/** Stops magnifying and clears what was set. */
export function detach(dock) {
    const handle = dock && handles.get(dock);

    if (!handle) {
        return;
    }

    dock.removeEventListener('pointermove', handle.onMove);
    dock.removeEventListener('pointerleave', handle.onLeave);
    handles.delete(dock);

    for (const item of dock.querySelectorAll('.veto-dock__item')) {
        item.style.removeProperty('--veto-dock-scale');
    }
}