# Veto.Switch

A toggle switch. It is two-state on a `bool` and three-state on a `bool?`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Switch
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Switch
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoSwitch TValue="bool"
            Label="Email notifications"
            HelperText="We will only mail you about things you asked for."
            @bind-Value="_notify" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/switch`. Source and issues: https://github.com/G-HK/Veto
