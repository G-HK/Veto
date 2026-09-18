# Veto.Slider

A slider for any numeric type, with marks, snapping and an optional second thumb for ranges.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Slider
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Slider
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoSlider TValue="int"
            Label="Volume"
            ShowValue="true"
            Suffix="%"
            @bind-Value="_volume" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/slider`. Source and issues: https://github.com/G-HK/Veto
