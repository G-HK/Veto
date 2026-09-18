# Veto.Layouts.DashboardLayout

A ready-made dashboard page: an app bar, a navigation drawer and a content area in one component.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.DashboardLayout
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.DashboardLayout
@using Veto.Layouts.Dashboard
@using Veto.Components.Navigation
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoDashboardLayout Title="Operations" Items="Nav">
    <VetoDashboard Columns="1" ColumnsLarge="4">…</VetoDashboard>
</VetoDashboardLayout>

@code {
    private static readonly IReadOnlyList<VetoNavEntry> Nav =
    [
        new() { Label = "Overview", Href = "/" },
        new() { Label = "Incidents", Href = "/incidents", Badge = "3" }
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts`. Source and issues: https://github.com/G-HK/Veto
