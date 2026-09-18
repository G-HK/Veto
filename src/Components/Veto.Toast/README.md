# Veto.Toast

Toast notifications. `VetoToastHost` renders them and `IVetoToastService` raises them from anywhere.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Toast
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Toast
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

Register the service in `Program.cs`:

```csharp
builder.Services.AddVetoToast();
```

## Usage

```razor
@* Once, in the layout: *@
<VetoToastHost />

@* Anywhere else: *@
@inject IVetoToastService Toasts

<button @onclick="Save">Save</button>

@code {
    private void Save() => Toasts.Success("Saved");
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/toast`. Source and issues: https://github.com/G-HK/Veto
