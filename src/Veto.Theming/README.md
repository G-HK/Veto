# Veto.Theming

Light, dark and system themes for Veto: `VetoThemeProvider`, the `IVetoThemeService` behind it,
and `VetoThemeToggle`. The chosen theme is remembered and applied before first paint, so the
page does not flash.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party
dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Theming
```

## Setup

Register the service in `Program.cs`:

```csharp
builder.Services.AddVetoTheming();
```

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Theming
```

Load the init script and the stylesheet inside `<head>` in `App.razor` or `index.html`. Put the
script first: it applies the saved theme before anything renders.

```html
<script src="_content/Veto.Theming/veto-theme-init.js"></script>
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
@* Once, in the layout: *@
<VetoThemeProvider />

@* Wherever the switch belongs: *@
<VetoThemeToggle ShowSystemOption="true" />
```

Your own colours, one palette for each mode. Every Veto component follows them, including one
pinned to a palette with `ColorScheme`:

```csharp
@inject IVetoThemeService Theme

await Theme.SetThemeAsync(new VetoTheme
{
    Light = new VetoPalette { Primary = "#0f766e", Surface = "#ffffff", Text = "#0f172a" },
    Dark = new VetoPalette { Primary = "#2dd4bf", Surface = "#0b1220", Text = "#e2e8f0" }
});
```

## Documentation

The provider and the tokens are documented on the Veto docs site at `/docs/theming/provider`.
Source and issues: https://github.com/G-HK/Veto
