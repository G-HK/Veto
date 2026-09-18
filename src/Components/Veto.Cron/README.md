# Veto.Cron

A visual cron expression builder, bound to a cron string, together with the parser, the plain-English describer and the next-run scheduler behind it.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Cron
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Cron
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

Descriptions are English by default. Pass your own `Describer` delegate to use another describer, such as `CronExpressionDescriptor` for other languages.

## Usage

```razor
<VetoCron @bind-Value="_schedule" />

@code {
    private string _schedule = "0 9 * * 1-5";
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/cron`. Source and issues: https://github.com/G-HK/Veto
