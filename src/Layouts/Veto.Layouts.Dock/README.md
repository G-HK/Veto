# Veto.Layouts.Dock

A dock of app icons that magnify under the pointer. It can sit on any edge and auto-hide.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layouts.Dock
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Layouts.Dock
@using Veto.Components.Icon
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoDock Label="Apps">
    <VetoDockItem Icon="@VetoIcons.Home" Label="Home" Href="/" />
    <VetoDockItem Icon="@VetoIcons.Search" Label="Search" Href="/search" />
    <VetoDockItem Icon="@VetoIcons.Settings" Label="Settings" Badge="2" Href="/settings" />
</VetoDock>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/layouts`. Source and issues: https://github.com/G-HK/Veto
