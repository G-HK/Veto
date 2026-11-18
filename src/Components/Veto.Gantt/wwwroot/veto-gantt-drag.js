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

// How far the pointer has to travel before a press counts as a drag rather than a click.
const SLOP = 3;

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

    const state = { options, mode: null, bar: null, startX: 0, startWidth: 0, captured: false };

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
        state.captured = false;

        bar.dataset.dragging = state.mode;
        event.preventDefault();
    };

    // The pointer is captured once it has travelled, not on the press. A press only becomes a
    // drag when it moves; until then it is a click, and capturing the pointer would hand that
    // click to the grid instead of the bar, which is what selects a row.
    const captureOnceMoving = event => {
        if (state.captured || Math.abs(event.clientX - state.startX) <= SLOP) {
            return;
        }

        grid.setPointerCapture(event.pointerId);
        state.captured = true;
    };

    const onPointerMove = event => {
        if (!state.mode) {
            const bar = barFrom(event);
            grid.style.cursor = bar
                ? (modeFor(bar, event) === 'move' ? 'grab' : 'ew-resize')
                : '';
            return;
        }

        // The button was let go somewhere the grid could not see - only possible before the
        // pointer is captured. Without this the bar would follow a pointer nobody is holding.
        if (!(event.buttons & 1)) {
            onCancel();
            return;
        }

        captureOnceMoving(event);

        // Still a click as far as anyone can tell, so nothing is previewed.
        if (!state.captured) {
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

        // A press that never became a drag moves nothing, however few pixels a day is at this zoom.
        const days = state.captured ? daysMoved(event) : 0;
        const mode = state.mode;
        const bar = state.bar;

        state.mode = null;
        state.bar = null;

        delete bar.dataset.dragging;

        // Cleared before .NET re-renders, so the component's own left and width take over
        // cleanly rather than fighting an inline transform.
        bar.style.transform = '';
        bar.style.width = '';

        if (state.captured) {
            state.captured = false;

            try {
                grid.releasePointerCapture(event.pointerId);
            } catch {
                // The capture may already be gone if the pointer left the window.
            }
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
        state.captured = false;
    };

    // A captured pointer keeps reporting to the grid wherever it goes. One that is not captured
    // yet is simply gone, and so is the press it started.
    const onPointerLeave = () => {
        if (state.mode && !state.captured) {
            onCancel();
        }
    };

    grid.addEventListener('pointerdown', onPointerDown);
    grid.addEventListener('pointermove', onPointerMove);
    grid.addEventListener('pointerup', finish);
    grid.addEventListener('pointercancel', onCancel);
    grid.addEventListener('pointerleave', onPointerLeave);

    handles.set(grid, { state, onPointerDown, onPointerMove, finish, onCancel, onPointerLeave, options });
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
    grid.removeEventListener('pointerleave', handle.onPointerLeave);

    handles.delete(grid);
    grid.style.cursor = '';
}
