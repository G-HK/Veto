# Veto.Layouts.Shell

The app shell to compose your own layout from: `VetoLayout`, `VetoAppBar`, `VetoDrawer` (persistent, temporary, mini or responsive), `VetoMainContent` and `VetoFooter`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.Shell
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.Shell
@using Veto.Components.Navigation
@using Veto.Components.Layout
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
@inherits LayoutComponentBase

<VetoLayout>
    <VetoAppBar>
        My app
        <VetoSpacer />
    </VetoAppBar>

    <VetoDrawer @bind-Open="_open">
        <VetoNavMenu Items="Nav" />
    </VetoDrawer>

    <VetoMainContent>
        @Body
    </VetoMainContent>
</VetoLayout>

@code {
    private bool _open = true;

    private static readonly IReadOnlyList<VetoNavEntry> Nav =
    [
        new() { Label = "Home", Href = "/" }
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts/shell`. Source and issues: https://github.com/G-HK/Veto
