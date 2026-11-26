# Veto.Icon

`VetoIcon` and `VetoIcons`, the built-in SVG icon set. No icon font and no external requests.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

Try it live in the docs site: <https://veto.blackpanda.be/docs/components/icon>

## Install

```powershell
dotnet add package Veto.Icon
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoIcon Icon="@VetoIcons.Search" />
```

## Credits

The industry, energy and work icons (`Factory`, `EvCharger`, `Wrench`, `HardHat`, `Network`, `ArrowRight` and the rest of that section) are from [Lucide](https://lucide.dev), ISC licence.

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/icon`. Source and issues: https://github.com/G-HK/Veto
