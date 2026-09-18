# Veto.Layouts.Dashboard

A responsive dashboard grid, `VetoDashboard`, and titled tiles, `VetoDashboardTile`, that span columns and rows.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.Dashboard
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.Dashboard
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoDashboard Columns="1" ColumnsMedium="2" ColumnsLarge="4">
    <VetoDashboardTile Title="Revenue" Icon="@VetoIcons.TrendUp" ColumnSpanLarge="2">
        …
    </VetoDashboardTile>
    <VetoDashboardTile Title="Uptime">…</VetoDashboardTile>
</VetoDashboard>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts`. Source and issues: https://github.com/G-HK/Veto
