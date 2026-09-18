# Veto.Core

The foundation every Veto component is built on: `VetoComponentBase` and `VetoInputBase<T>`, the
CSS and style builders, the shared enums (`VetoColor`, `VetoVariant`, `VetoSpacing`, …) and the
design tokens stylesheet.

You rarely install this directly. Every Veto component package depends on it, and the `Veto`
package brings in everything. Install it on its own to build your own components on the same
contract and tokens.

## Install

```powershell
dotnet add package Veto.Core
```

## Setup

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`. Every Veto component
reads its colours, spacing and radii from the tokens defined in it:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

Style your own markup with the same tokens, so it follows the theme:

```css
.my-panel {
    color: var(--veto-color-text);
    background: var(--veto-color-surface-raised);
    border: 1px solid var(--veto-color-border);
    border-radius: var(--veto-radius-md);
}
```

Every Veto tooltip layers at one token, so a single line moves them all above your own
overlays:

```css
:root { --veto-z-tooltip: 2000; }
```

## Documentation

The design tokens are documented on the Veto docs site at `/docs/theming/tokens`. Source and
issues: https://github.com/G-HK/Veto
