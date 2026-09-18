# Veto.Gantt

An interactive Gantt chart: nested tasks, drag to reschedule, zoomable time scale, selection, export, and tooltips you can reword or replace with your own card. View state can persist per user.

Part of [Veto](https://github.com/G-HK/Veto), a Blazor component library with no third-party dependencies. Install the `Veto` package instead to get every component at once.

## Install

```powershell
dotnet add package Veto.Gantt
```

## Setup

Add the namespaces to `_Imports.razor`:

```razor
@using Veto.Core
@using Veto.Components.Gantt
```

Link the stylesheet once, inside `<head>` in `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Usage

```razor
<VetoGantt Items="Tasks"
           @bind-Scale="_scale"
           @bind-SelectedId="_selectedId"
           Height="26rem">
    @* Optional: your own hover card. TooltipText="..." rewrites the plain tooltip instead,
       and ShowTooltips="false" turns tooltips off. *@
    <TooltipTemplate Context="task">
        <strong>@task.Name</strong>
        <div>@task.DurationDays days · @task.Category</div>
    </TooltipTemplate>
</VetoGantt>

@* Drive it from code through an @ref: SelectAsync, Expand/Collapse(All), SetScaleAsync,
   ScrollToTodayAsync, ScrollToDateAsync, ExportAsync, ShowTooltip/HideTooltip, ResetAsync. *@
<button @onclick="() => _chart.CollapseAll()">Collapse all</button>
<VetoGantt @ref="_chart" Items="Tasks" />

@code {
    private VetoGantt _chart = default!;

    private static readonly IReadOnlyList<VetoGanttItem> Tasks =
    [
        new() { Id = "platform", Name = "Platform",
                Start = new(2026, 3, 2), End = new(2026, 4, 10),
                Category = "Engineering", Progress = 0.55 },
        new() { Id = "design", Name = "Design system", ParentId = "platform",
                Start = new(2026, 3, 2), End = new(2026, 3, 13),
                Category = "Design", Progress = 1 }
    ];
}
```

## Documentation

Every parameter, with a live demo, is documented on the Veto docs site at `/docs/components/gantt`. Source and issues: https://github.com/G-HK/Veto
