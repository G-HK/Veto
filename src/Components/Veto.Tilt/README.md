# Veto.Tilt

A wrapper that tilts its content in 3D toward the pointer on hover.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Tilt
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Tilt
@using Veto.Components.Layout
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoTilt3D>
    <VetoCard Title="Hover me" Elevation="VetoElevation.Raised">
        The whole card is the tilt surface.
    </VetoCard>
</VetoTilt3D>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/tilt`. Source and issues: https://github.com/G-HK/Veto
