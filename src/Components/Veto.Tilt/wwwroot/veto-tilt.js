// Pointer tracking for VetoTilt3D.
//
// The listeners live here rather than in C# on purpose: a pointermove handler that crosses
// into .NET fires dozens of times a second, which costs frames in WebAssembly and is far worse
// over a Server circuit. JavaScript writes two custom properties and CSS does the rest, so the
// transform happens on the compositor and .NET is never involved in the animation.

const handles = new WeakMap();

function prefersReducedMotion() {
    return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Starts tracking the pointer over an element.
 * @param {HTMLElement} element the tilt surface
 * @param {{maxTilt: number, scale: number, glare: boolean, reset: boolean, reverse: boolean, axis: string}} options
 */
export function attach(element, initial) {
    if (!element || handles.has(element)) {
        return;
    }

    // Tilt follows a pointer position, which a touch or a pen does not meaningfully provide -
    // and someone who asked for less motion should not get a surface that moves at all.
    if (prefersReducedMotion() || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
        return;
    }

    // One options object for the element's lifetime, mutated by update() - the handlers read
    // it on every event, so a changed parameter takes effect on the next pointer move.
    const options = { ...initial };

    const onMove = event => {
        const rect = element.getBoundingClientRect();

        if (rect.width === 0 || rect.height === 0) {
            return;
        }

        // -0.5 … 0.5 from the centre of the element, whatever its size.
        const px = (event.clientX - rect.left) / rect.width - 0.5;
        const py = (event.clientY - rect.top) / rect.height - 0.5;

        // Y tilt follows horizontal movement and X tilt follows vertical, which is what makes
        // the surface look like it is being pushed rather than dragged. Reverse makes it lean
        // towards the pointer instead; an axis lock zeroes the other one.
        const sign = options.reverse ? -1 : 1;
        const y = options.axis === 'vertical' ? 0 : sign * px * options.maxTilt * 2;
        const x = options.axis === 'horizontal' ? 0 : sign * -py * options.maxTilt * 2;
        element.style.setProperty('--veto-tilt-y', `${y}deg`);
        element.style.setProperty('--veto-tilt-x', `${x}deg`);

        if (options.glare) {
            element.style.setProperty('--veto-tilt-glare-x', `${(px + 0.5) * 100}%`);
            element.style.setProperty('--veto-tilt-glare-y', `${(py + 0.5) * 100}%`);
            element.style.setProperty('--veto-tilt-glare-opacity', '1');
        }
    };

    const onEnter = () => {
        element.style.setProperty('--veto-tilt-scale', String(options.scale));
        element.dataset.tilting = 'true';
    };

    const onLeave = () => {
        delete element.dataset.tilting;
        element.style.setProperty('--veto-tilt-scale', '1');
        element.style.setProperty('--veto-tilt-glare-opacity', '0');

        if (options.reset) {
            element.style.setProperty('--veto-tilt-x', '0deg');
            element.style.setProperty('--veto-tilt-y', '0deg');
        }
    };

    element.addEventListener('pointerenter', onEnter);
    element.addEventListener('pointermove', onMove);
    element.addEventListener('pointerleave', onLeave);

    handles.set(element, { onEnter, onMove, onLeave, options });
}

/** Applies changed parameters to an attached element without re-attaching it. */
export function update(element, changed) {
    const handle = element && handles.get(element);

    if (!handle) {
        return;
    }

    Object.assign(handle.options, changed);

    if (element.dataset.tilting) {
        element.style.setProperty('--veto-tilt-scale', String(handle.options.scale));
    }

    if (!handle.options.glare) {
        element.style.setProperty('--veto-tilt-glare-opacity', '0');
    }
}

/** Stops tracking and clears everything the tilt set. */
export function detach(element) {
    const handle = element && handles.get(element);

    if (!handle) {
        return;
    }

    element.removeEventListener('pointerenter', handle.onEnter);
    element.removeEventListener('pointermove', handle.onMove);
    element.removeEventListener('pointerleave', handle.onLeave);
    handles.delete(element);

    for (const property of ['--veto-tilt-x', '--veto-tilt-y', '--veto-tilt-scale',
                            '--veto-tilt-glare-x', '--veto-tilt-glare-y', '--veto-tilt-glare-opacity']) {
        element.style.removeProperty(property);
    }

    delete element.dataset.tilting;
}
