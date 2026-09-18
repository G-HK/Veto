# Veto.TerminalBox

A terminal window for landing pages and installation guides, with an optional typing animation and a copy button that leaves out the prompt. It only displays text and runs nothing.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.TerminalBox
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.TerminalBox
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

The copy button confirms with a toast when `Veto.Toast` is registered (`builder.Services.AddVetoToast()` plus one `<VetoToastHost />` in the layout). Without it the copy still works, silently.

## Usage

```razor
<VetoTerminalBox Command="dotnet add package Veto" Typed="true" />

<VetoTerminalBox Title="bash">
    <VetoTerminalLine Command="dotnet run" />
    <VetoTerminalLine Prompt="@null" Output="Now listening on: http://localhost:5000" />
</VetoTerminalBox>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/terminal-box`. Source and issues: https://github.com/G-HK/Veto
