# Veto.Kbd

Keyboard keys and shortcuts in text: `VetoKbd`, `VetoShortcut` and `VetoShortcutList`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Kbd
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Kbd
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
Press <VetoKbd Keys="Esc" /> to close, or
<VetoKbd Keys="Ctrl+Shift+P" /> to open the command palette.
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/kbd`. Source and issues: https://github.com/G-HK/Veto
