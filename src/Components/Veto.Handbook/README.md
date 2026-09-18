# Veto.Handbook

A long-form documentation page built from a list of sections, with `VetoHandbookToc` for the table of contents that tracks where the reader is.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Handbook
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Handbook
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoHandbookPage Eyebrow="Guides"
                  Title="Getting started"
                  Lead="Install the package and render your first component."
                  Sections="Sections" />

@code {
    private static readonly IReadOnlyList<VetoHandbookSection> Sections =
    [
        new() { Id = "install", Title = "Install", Body = "…" },
        new() { Id = "register", Title = "Register the services", Body = "…" }
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/handbook-page`. Source and issues: https://github.com/G-HK/Veto
