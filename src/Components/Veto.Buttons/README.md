# Veto.Buttons

`VetoButton`, `VetoIconButton`, `VetoButtonGroup` and `VetoButtonReveal`: filled, outlined and text variants, icon-only buttons with an accessible label, grouped buttons and toolbars, and a button that reveals more actions.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Buttons
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Buttons
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoButton Variant="VetoVariant.Filled">Save changes</VetoButton>
<VetoButton Variant="VetoVariant.Outlined">Cancel</VetoButton>

<VetoIconButton Icon="@VetoIcons.Trash" Label="Delete" Color="VetoColor.Danger" />

<VetoButtonGroup Label="View mode">
    <VetoButton Variant="VetoVariant.Outlined">Day</VetoButton>
    <VetoButton Variant="VetoVariant.Outlined">Week</VetoButton>
</VetoButtonGroup>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/button`. Source and issues: https://github.com/G-HK/Veto
