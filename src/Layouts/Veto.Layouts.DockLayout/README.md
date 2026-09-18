# Veto.Layouts.DockLayout

A ready-made desktop-style page: a wallpaper, your content and a dock.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.DockLayout
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.DockLayout
@using Veto.Layouts.Dock
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoDockLayout DockLabel="Apps">
    <Items>
        <VetoDockItem Icon="@VetoIcons.Home" Label="Home" Href="/" />
        <VetoDockItem Icon="@VetoIcons.Settings" Label="Settings" Href="/settings" />
    </Items>
    <ChildContent>
        Page content.
    </ChildContent>
</VetoDockLayout>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts`. Source and issues: https://github.com/G-HK/Veto
