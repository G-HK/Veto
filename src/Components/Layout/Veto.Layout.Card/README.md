# Veto.Layout.Card

A card surface for grouping related content, with optional composed sections: header, title, description, content and footer.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Layout.Card
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Layout
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoCard Padding="VetoSpacing.None">
    <VetoCardHeader>
        <VetoCardTitle>Deploy to production</VetoCardTitle>
        <VetoCardDescription>Three commits are waiting.</VetoCardDescription>
    </VetoCardHeader>

    <VetoCardContent>
        <p>Deploying runs the full test suite first.</p>
    </VetoCardContent>

    <VetoCardFooter Justify="VetoJustify.End">
        Footer actions go here.
    </VetoCardFooter>
</VetoCard>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/card`. Source and issues: https://github.com/G-HK/Veto
