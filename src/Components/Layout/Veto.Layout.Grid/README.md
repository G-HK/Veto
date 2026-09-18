# Veto.Layout.Grid

A responsive CSS grid with per-breakpoint column counts, plus `VetoGridItem` for spans.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layout.Grid
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Layout
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoGrid Columns="1" ColumnsSmall="2" ColumnsLarge="4" Gap="VetoSpacing.Medium">
    <VetoCard>1</VetoCard>
    <VetoCard>2</VetoCard>
    <VetoCard>3</VetoCard>
    <VetoCard>4</VetoCard>
</VetoGrid>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/grid`. Source and issues: https://github.com/G-HK/Veto
