// Theme interop for IVetoThemeService.
//
// The document attribute is the single source of truth for appearance:
//   <html data-veto-theme="dark">   forced dark
//   <html data-veto-theme="light">  forced light
//   no attribute                    follow prefers-color-scheme
//
// veto-theme-init.js applies the stored mode before first paint. This module takes
// over afterwards and keeps the attribute, localStorage and .NET in step.

const STORAGE_KEY = 'veto-theme-mode';

let query = null;
let listener = null;

function apply(mode) {
    const root = document.documentElement;

    if (mode === 'Light' || mode === 'Dark') {
        root.setAttribute('data-veto-theme', mode.toLowerCase());
    }
    else
    {
        root.removeAttribute('data-veto-theme');
    }
}

function store(mode) {
    try {
        if (mode === 'System') {
            localStorage.removeItem(STORAGE_KEY);
        }
        else
        {
            localStorage.setItem(STORAGE_KEY, mode);
        }
    } catch {
        // Private windows and blocked site data throw on access. Theme still applies for
        // this page; it just will not be remembered.
    }
}

function storedMode() {
    try {
        const value = localStorage.getItem(STORAGE_KEY);
        return value === 'Light' || value === 'Dark' ? value : 'System';
    }
    catch
    {
        return 'System';
    }
}

function prefersDark() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/**
 * Reads the persisted mode, applies it, and subscribes to OS preference changes.
 * Returns the state .NET needs to render the first time.
 */
export function initialize(dotNetRef) {
    const mode = storedMode();
    apply(mode);

    if (!listener && window.matchMedia) {
        query = window.matchMedia('(prefers-color-scheme: dark)');
        listener = e => dotNetRef.invokeMethodAsync('OnSystemPreferenceChanged', e.matches);
        query.addEventListener('change', listener);
    }

    return { mode, prefersDark: prefersDark() };
}

/** Applies a mode to the document and persists it. */
export function setMode(mode) {
    apply(mode);
    store(mode);
    return prefersDark();
}

/** Drops the OS preference subscription. */
export function dispose() {
    if (query && listener) {
        query.removeEventListener('change', listener);
    }

    query = null;
    listener = null;
}