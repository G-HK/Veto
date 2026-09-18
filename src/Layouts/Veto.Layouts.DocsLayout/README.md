# Veto.Layouts.DocsLayout

A ready-made documentation page: an app bar, a navigation drawer, the page and an optional table of contents.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.DocsLayout
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.DocsLayout
@using Veto.Components.Navigation
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
@inherits LayoutComponentBase

<VetoDocsLayout Items="Nav">
    <Brand>My library</Brand>
    <ChildContent>@Body</ChildContent>
</VetoDocsLayout>

@code {
    private static readonly IReadOnlyList<VetoNavEntry> Nav =
    [
        new() { Label = "Getting started", Href = "/docs" },
        new() { Label = "Components", Children =
        [
            new() { Label = "Button", Href = "/docs/button" }
        ] }
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts`. Source and issues: https://github.com/G-HK/Veto
