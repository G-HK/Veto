// Pointer dragging for VetoGantt bars.
//
// One delegated listener on the grid rather than a handler per bar. Bars come and go as rows
// are filtered, collapsed and re-sorted, and per-bar handlers would have to be attached and
// detached in step with every render; delegation survives all of it and has nothing to keep
// in sync.
//
// The drag itself is handled here so .NET is not called on every pointermove - dozens of interop
// round trips a second would stutter in WebAssembly and be far worse over a Server circuit.
// JavaScript previews the move with a transform and calls back exactly once, on release.

const handles = new WeakMap();

// How close to an edge counts as a resize rather than a move.
const EDGE = 6;

/**
 * Makes every editable bar inside a grid draggable.
 * @param {HTMLElement} grid the element containing the bars
 * @param {object} dotNet reference to the owning component
 * @param {{pixelsPerDay: number, snapDays: number, resizable: boolean}} options
 */
export function attach(grid, dotNet, options) {
    if (!grid) {
        return;
    }

    // Re-attaching replaces the previous options, which is how a scale change takes effect.
    if (handles.has(grid)) {
        handles.get(grid).options = options;
        return;
    }

    const state = { options, mode: null, bar: null, startX: 0, startWidth: 0 };

    const barFrom = event => {
        const bar = event.target.closest('.veto-gantt__bar[data-editable="true"]');
        return bar && grid.contains(bar) ? bar : null;
    };

    const modeFor = ( bar, event ) => {
        if (!state.options.resizable) {
            return 'move';
        }

        const box = bar.getBoundingClientRect();

        if (event.clientX - box.left <= EDGE) {
            return 'start';
        }

        if (box.right - event.clientX <= EDGE) {
            return 'end';
        }

        return 'move';
    };

    const daysMoved = event => {
        const raw = (event.clientX - state.startX) / state.options.pixelsPerDay;

        // Snapped to whole units of the current zoom, so a bar cannot land part way through a
        // day - and at month scale cannot land on an arbitrary date.
        return Math.round(raw / state.options.snapDays) * state.options.snapDays;
    };

    const onPointerDown = event => {
        // Primary button only: a right-click should open a menu, not start a drag.
        if (event.button !== 0) {
            return;
        }

        const bar = barFrom(event);

        if (!bar) {
            return;
        }

        state.bar = bar;
        state.mode = modeFor(bar, event);
        state.startX = event.clientX;
        state.startWidth = bar.getBoundingClientRect().width;

        bar.dataset.dragging = state.mode;
        grid.setPointerCapture(event.pointerId);
        event.preventDefault();
    };

    const onPointerMove = event => {
        if (!state.mode) {
            const bar = barFrom(event);
            grid.style.cursor = bar
                ? (modeFor(bar, event) === 'move' ? 'grab' : 'ew-resize')
                : '';
            return;
        }

        const offset = daysMoved(event) * state.options.pixelsPerDay;
        const minimum = state.options.pixelsPerDay;

        // Previewed with a transform rather than by changing left and width, so nothing reflows
        // while the pointer is down.
        if (state.mode === 'move') {
            state.bar.style.transform = `translate(${offset}px, -50%)`;
        } else if (state.mode === 'start') {
            state.bar.style.transform = `translate(${offset}px, -50%)`;
            state.bar.style.width = `${Math.max(minimum, state.startWidth - offset)}px`;
        } else {
            state.bar.style.width = `${Math.max(minimum, state.startWidth + offset)}px`;
        }
    };

    const finish = async event => {
        if (!state.mode) {
            return;
        }

        const days = daysMoved(event);
        const mode = state.mode;
        const bar = state.bar;

        state.mode = null;
        state.bar = null;

        delete bar.dataset.dragging;

        // Cleared before .NET re-renders, so the component's own left and width take over
        // cleanly rather than fighting an inline transform.
        bar.style.transform = '';
        bar.style.width = '';

        try {
            grid.releasePointerCapture(event.pointerId);
        } catch {
            // The capture may already be gone if the pointer left the window.
        }

        if (days !== 0) {
            await dotNet.invokeMethodAsync('OnBarDragged', bar.dataset.rowId, mode, days);
        }
    };

    const onCancel = () => {
        if (state.bar) {
            delete state.bar.dataset.dragging;
            state.bar.style.transform = '';
            state.bar.style.width = '';
        }

        state.mode = null;
        state.bar = null;
    };

    grid.addEventListener('pointerdown', onPointerDown);
    grid.addEventListener('pointermove', onPointerMove);
    grid.addEventListener('pointerup', finish);
    grid.addEventListener('pointercancel', onCancel);

    handles.set(grid, { state, onPointerDown, onPointerMove, finish, onCancel, options });
}

/** Stops dragging inside a grid. */
export function detach(grid) {
    const handle = grid && handles.get(grid);

    if (!handle) {
        return;
    }

    grid.removeEventListener('pointerdown', handle.onPointerDown);
    grid.removeEventListener('pointermove', handle.onPointerMove);
    grid.removeEventListener('pointerup', handle.finish);
    grid.removeEventListener('pointercancel', handle.onCancel);

    handles.delete(grid);
    grid.style.cursor = '';
}
