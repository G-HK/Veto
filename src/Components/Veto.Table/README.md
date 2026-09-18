# Veto.Table

A templated table: you supply the header, a row template and an optional footer.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Table
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Table
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<Table TItem="Person" Items="People">
    <HeaderTemplate>
        <th>Name</th>
        <th>Role</th>
    </HeaderTemplate>
    <RowTemplate>
        <td>@context.Name</td>
        <td>@context.Role</td>
    </RowTemplate>
</Table>
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site. Source and issues: https://github.com/G-HK/Veto
