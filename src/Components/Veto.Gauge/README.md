# Veto.Gauge

A radial gauge with a label, a unit and coloured bands for ranges.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Gauge
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Gauge
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoGauge Value="_cpu" Label="CPU" Unit="%" />

@code {
    private double _cpu = 42;
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/gauge`. Source and issues: https://github.com/G-HK/Veto
