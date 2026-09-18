# Veto.Feedback

Loading and progress feedback: `VetoSpinner`, `VetoProgress` (bar or ring, determinate or not), `VetoSkeleton` placeholders and `VetoLoadingOverlay`.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Feedback
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Feedback
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoSpinner Label="Checking availability" />

<VetoProgress Value="_done" Max="100" ShowValue="true" />

<VetoSkeleton Lines="3" />

<VetoLoadingOverlay Loading="_saving" Label="Saving">
    The content being covered.
</VetoLoadingOverlay>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/loading`. Source and issues: https://github.com/G-HK/Veto
