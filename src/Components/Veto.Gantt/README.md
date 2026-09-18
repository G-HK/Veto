# Veto.Gantt

An interactive Gantt chart that opens on today's work, to the minute: nested tasks, or many jobs on one line grouped by server with overlaps stacked, drag to reschedule, zoom from hours to months, short ranges that fill the width, filters of your own, selection, export, and tooltips you can reword or replace with your own card.

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
@* Opens on today by default: only today's work, stretched across the full width.
   View="VetoGanttView.Schedule" opens the whole plan; a short Start/End range also fills
   the width (FitToWidth="false" turns that off). *@
<VetoGantt Items="Tasks"
           View="VetoGanttView.Schedule"
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

@* Many jobs on one line: a parent's sub-items side by side on its own row. The fold arrow
   still opens them into rows; Collapsible="false" keeps them on one line. *@
<VetoGantt Items="Pipelines" SubItemLayout="VetoGanttSubItemLayout.Inline" />

@* Timed jobs: StartTime/EndTime on an item place it to the minute, and the today view is
   drawn in hours. StartTime/EndTime on the chart trim the day to a window. *@
<VetoGantt Items="Jobs" StartTime="new TimeOnly(14, 0)" EndTime="new TimeOnly(20, 0)" />

@* An operations schedule: runs from exact timestamps, one line per server, overlapping
   runs stacked into lanes. TodayScale="VetoGanttScale.QuarterHour" for 15-minute columns.
   Today's date sits on its own line (DateLineFormat, ShowDateLine), the now marker carries
   the time (NowLabelFormat, ShowNowLabel) and follows the clock, and the timeline scrolls
   itself to it. Rows can differ in height (RowHeight on an item, GroupRowHeight), and the
   hover card layers at TooltipZIndex - it is placed against the window, so the chart's
   scrolling never cuts it off. *@
<VetoGantt Items="Runs"
           GroupBy="run => run.Category"
           SubItemLayout="VetoGanttSubItemLayout.Inline"
           GroupRowHeight="44"
           DateLineFormat="ddd d MMM" />

@* Filters of your own, bound both ways, with one event for any change. Scales and
   ExportFormats choose which zoom levels and export formats are offered. *@
<VetoGantt Items="Tasks"
           Filters="LateFilter"
           @bind-HiddenCategories="_hidden"
           OnFilterChanged="state => _rows = state.VisibleItems.Count"
           Scales="[VetoGanttScale.Day]"
           ExportFormats="[VetoGanttExportFormat.Csv]" />

@code {
    private VetoGantt _chart = default!;
    private IReadOnlyCollection<string> _hidden = [];
    private int _rows;

    // Pipelines and Jobs are lists like Tasks below: jobs under a parent, and items with times.

    // Runs come straight from timestamps - DateTime or DateTimeOffset - with the server in Category.
    private IReadOnlyList<VetoGanttItem> Runs => apiRuns
        .Select(run => new VetoGanttItem(run.Id, run.Name, run.StartedAt, run.FinishedAt) { Category = run.Server })
        .ToList();

    private static readonly VetoGanttFilter[] LateFilter =
    [
        new("late", "Late", item => item.EndsAt < DateTime.Now && item.Progress < 1)
    ];

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
