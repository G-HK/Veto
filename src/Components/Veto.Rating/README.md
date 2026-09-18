# Veto.Rating

A star rating input that works with any icon, supports half values and binds to any numeric type.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Rating
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Rating
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoRating TValue="int"
            Label="How was it?"
            ShowValue="true"
            @bind-Value="_score" />
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/rating`. Source and issues: https://github.com/G-HK/Veto
