# Veto.MagicTextBox

A multi-line text box that holds a fixed height and offers to expand when the text outruns it. It has corner slots and a copy button.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.MagicTextBox
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.MagicTextBox
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

The copy button confirms with a toast when `Veto.Toast` is registered (`builder.Services.AddVetoToast()` plus one `<VetoToastHost />` in the layout). Without it the copy still works, silently.

## Usage

```razor
<VetoMagicTextBox TValue="string"
                  Label="Release notes"
                  CollapsedLines="3"
                  ExpandedLines="10"
                  @bind-Value="_notes" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/magic-text-box`. Source and issues: https://github.com/G-HK/Veto
