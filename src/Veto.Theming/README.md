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

## Documentation

The provider and the tokens are documented on the Veto docs site at `/docs/theming/provider`.
Source and issues: https://github.com/G-HK/Veto
