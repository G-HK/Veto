# Veto.Menu

A dropdown menu: `VetoMenu`, `VetoMenuItem` (icon, shortcut, destructive styling), `VetoMenuLabel` and `VetoMenuDivider`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Menu
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Menu
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoMenu Label="Actions" Icon="@VetoIcons.Bolt">
    <VetoMenuItem Icon="@VetoIcons.Edit" Text="Rename" Shortcut="F2" OnClick="Rename" />
    <VetoMenuItem Icon="@VetoIcons.Copy" Text="Duplicate" Shortcut="Ctrl+D" OnClick="Duplicate" />
    <VetoMenuDivider />
    <VetoMenuItem Icon="@VetoIcons.Trash" Text="Delete" Destructive="true" OnClick="Delete" />
</VetoMenu>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/menu`. Source and issues: https://github.com/G-HK/Veto
