# Veto.Layout.Spacer

Fixed space between two elements, or, inside a flex row, a spring that pushes what follows to the far end.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layout.Spacer
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
<div style="display: flex">
    <span>My app</span>
    <VetoSpacer />
    <span>Pushed to the far end</span>
</div>

<p>First paragraph.</p>
<VetoSpacer Size="VetoSpacing.Large" />
<p>Second paragraph, a large step below.</p>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts/shell`. Source and issues: https://github.com/G-HK/Veto
