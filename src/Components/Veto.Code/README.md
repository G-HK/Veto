# Veto.Code

A syntax-highlighted code block with a copy button.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Code
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Code
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

The copy button confirms with a toast when `Veto.Toast` is registered (`builder.Services.AddVetoToast()` plus one `<VetoToastHost />` in the layout). Without it the copy still works, silently.

## Usage

```razor
<VetoCode Language="csharp" Code="@Sample" />

@code {
    private const string Sample = "builder.Services.AddVeto();";
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site. Source and issues: https://github.com/G-HK/Veto
