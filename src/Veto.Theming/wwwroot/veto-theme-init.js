// Anti-flash theme boot.
//
// Add this to <head>, as a plain synchronous script, BEFORE any stylesheet link:
//
//   <script src="_content/Veto.Theming/veto-theme-init.js"></script>
//
// It applies the remembered theme before the first paint. Without it a prerendered page
// renders light, then snaps to dark once WebAssembly boots - visible on every navigation.
// It must not be deferred, async, or a module: all three run after first paint.
(function () {
    try {
        var mode = localStorage.getItem('veto-theme-mode');

        if (mode === 'Light' || mode === 'Dark') {
            document.documentElement.setAttribute('data-veto-theme', mode.toLowerCase());
        }
    } catch (e) {
        // No stored preference available; fall back to prefers-color-scheme.
    }
})();