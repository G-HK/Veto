# Veto.BlinkText

Reveals text a character at a time behind a blinking caret. Changing `Text` restarts the reveal.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.BlinkText
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.BlinkText
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoBlinkText Text="Initialising the component library."
               CharactersPerSecond="34"
               OnCompleted="OnDone" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/blink-text`. Source and issues: https://github.com/G-HK/Veto
