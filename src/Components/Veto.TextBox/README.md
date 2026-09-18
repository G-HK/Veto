# Veto.TextBox

A text input with a label, helper text and validation, bound to any type that can be parsed from text.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.TextBox
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.TextBox
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoTextBox TValue="string"
             Label="Project name"
             Placeholder="e.g. Veto"
             HelperText="Shown until the field becomes invalid."
             @bind-Value="_name" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/text-box`. Source and issues: https://github.com/G-HK/Veto
