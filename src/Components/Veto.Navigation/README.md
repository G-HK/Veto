# Veto.Navigation

`VetoLink` (active-route aware), `VetoNavMenu` (driven by a list of `VetoNavEntry`, with groups and badges) and `VetoBreadcrumbs`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Navigation
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Navigation
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoNavMenu Items="Nav" />

<VetoBreadcrumbs Items="Trail" />

@code {
    private static readonly IReadOnlyList<VetoNavEntry> Nav =
    [
        new() { Label = "Home", Href = "/", Icon = VetoIcons.Home },
        new() { Label = "Docs", Href = "/docs" }
    ];

    private static readonly VetoBreadcrumbItem[] Trail =
    [
        new("Home", "/", VetoIcons.Home),
        new("Docs", "/docs"),
        new("Breadcrumbs")          // no Href: this is where you are
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/link`. Source and issues: https://github.com/G-HK/Veto
