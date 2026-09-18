# Veto.Layout.Stack

Lays its children out in a row or a column with consistent spacing.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layout.Stack
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
<VetoStack Orientation="VetoOrientation.Horizontal" Gap="VetoSpacing.Medium">
    <div>One</div>
    <div>Two</div>
</VetoStack>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/stack`. Source and issues: https://github.com/G-HK/Veto
