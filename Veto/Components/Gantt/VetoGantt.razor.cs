using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Components.Gantt;

/// <summary>
/// A Gantt chart: a tree of projects and tasks on the left, a scrollable timeline on the right.
/// Filters, zoom, date navigation and export are built in.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="Editable"/> set, bars can be dragged to reschedule and their edges dragged
/// to resize, with the same moves on the keyboard. Give <see cref="PersistKey"/> a value and
/// each reader's edits are kept in their own browser; <see cref="OnItemChanged"/> is there for
/// an app that would rather store them itself.
/// </para>
/// <para>
/// Everything on the timeline is positioned by whole days multiplied by a per-scale pixel
/// figure, so switching zoom moves one number and nothing else.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoGantt Items="Tasks" @@bind-Scale="_scale" OnItemSelected="Open" /&gt;
/// </code>
/// </example>
public partial class VetoGantt : VetoComponentBase, IAsyncDisposable
{
    private readonly HashSet<string> _collapsed = [];
    private readonly HashSet<string> _hiddenCategories = [];

    private readonly Dictionary<string, (DateOnly Start, DateOnly End)> _overrides = [];

    private IJSObjectReference? _module;
    private IJSObjectReference? _dragModule;
    private IJSObjectReference? _storeModule;
    private DotNetObjectReference<VetoGantt>? _selfRef;
    private ElementReference _scroller;
    private ElementReference _grid;
    private bool _dragAttached;
    private bool _storeOpened;
    private string _storeBackend = "none";

    /// <inheritdoc />
    protected override string RootClass => "veto-gantt";

    /// <summary>The rows to chart. Hierarchy comes from each item's <c>ParentId</c>.</summary>
    [Parameter]
    public IReadOnlyList<VetoGanttItem> Items { get; set; } = [];

    /// <summary>How much time one column covers. Supports <c>@bind-Scale</c>.</summary>
    [Parameter]
    public VetoGanttScale Scale { get; set; } = VetoGanttScale.Day;

    /// <summary>Raised when the scale changes.</summary>
    [Parameter]
    public EventCallback<VetoGanttScale> ScaleChanged { get; set; }

    /// <summary>The text filter applied to row names. Supports <c>@bind-Search</c>.</summary>
    [Parameter]
    public string? Search { get; set; }

    /// <summary>Raised when the text filter changes.</summary>
    [Parameter]
    public EventCallback<string?> SearchChanged { get; set; }

    /// <summary>The id of the selected row. Supports <c>@bind-SelectedId</c>.</summary>
    [Parameter]
    public string? SelectedId { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback<string?> SelectedIdChanged { get; set; }

    /// <summary>Raised when a bar is activated, with the row it belongs to.</summary>
    [Parameter]
    public EventCallback<VetoGanttItem> OnItemSelected { get; set; }

    /// <summary>
    /// Raised when an export is requested, before anything is downloaded. Set
    /// <see cref="VetoGanttExportRequest.Handled"/> to take it over - to send the rows to a
    /// server, or produce a format the component does not.
    /// </summary>
    [Parameter]
    public EventCallback<VetoGanttExportRequest> OnExport { get; set; }

    /// <summary>The first day on the axis. Worked out from the items when null.</summary>
    [Parameter]
    public DateOnly? Start { get; set; }

    /// <summary>The last day on the axis. Worked out from the items when null.</summary>
    [Parameter]
    public DateOnly? End { get; set; }

    /// <summary>
    /// The day treated as today, for the marker and the Today button. Defaults to the local
    /// date; set it explicitly to keep a screenshot or a test stable.
    /// </summary>
    [Parameter]
    public DateOnly? TodayOverride { get; set; }

    /// <summary>The heading above the row names.</summary>
    [Parameter]
    public string SidebarTitle { get; set; } = "Tasks";

    /// <summary>Shown in place of the rows when the filters match nothing.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "No tasks match these filters.";

    /// <summary>The width of the panel on the left, as a CSS length.</summary>
    [Parameter]
    public string SidebarWidth { get; set; } = "18rem";

    /// <summary>
    /// The height of one row, in pixels. A number rather than a CSS length because the
    /// dependency arrows are drawn from it, and they need to agree with the rows exactly.
    /// </summary>
    [Parameter]
    public double RowHeight { get; set; } = 36;

    /// <summary>The chart's overall height. Null lets it grow to fit its rows.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>The colour of a bar with no colour of its own. Any CSS colour.</summary>
    [Parameter]
    public string BarColor { get; set; } = "var(--veto-color-primary)";

    /// <summary>Whether to show the toolbar at all.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>Whether the toolbar includes the text filter.</summary>
    [Parameter]
    public bool ShowSearch { get; set; } = true;

    /// <summary>Whether the toolbar includes a toggle per category.</summary>
    [Parameter]
    public bool ShowCategoryFilter { get; set; } = true;

    /// <summary>Whether the toolbar includes the Day / Week / Month picker.</summary>
    [Parameter]
    public bool ShowScalePicker { get; set; } = true;

    /// <summary>Whether the toolbar includes the Today and earlier/later buttons.</summary>
    [Parameter]
    public bool ShowNavigation { get; set; } = true;

    /// <summary>Whether the toolbar includes the CSV and JSON buttons.</summary>
    [Parameter]
    public bool Exportable { get; set; } = true;

    /// <summary>Whether a line marks today on the timeline.</summary>
    [Parameter]
    public bool ShowToday { get; set; } = true;

    /// <summary>Whether Saturdays and Sundays are shaded. Only visible at day scale.</summary>
    [Parameter]
    public bool ShowWeekends { get; set; } = true;

    /// <summary>Whether bars carry a progress fill.</summary>
    [Parameter]
    public bool ShowProgress { get; set; } = true;

    /// <summary>Whether bars carry their row's name.</summary>
    [Parameter]
    public bool ShowBarLabels { get; set; } = true;

    /// <summary>Whether the panel on the left shows each row's dates.</summary>
    [Parameter]
    public bool ShowDates { get; set; } = true;

    /// <summary>
    /// How rows are ordered. Source order by default, because the order an app supplies rows in
    /// usually means something the chart does not know about.
    /// </summary>
    [Parameter]
    public VetoGanttSort SortBy { get; set; } = VetoGanttSort.Source;

    /// <summary>Reverses whatever <see cref="SortBy"/> produces.</summary>
    [Parameter]
    public bool SortDescending { get; set; }

    /// <summary>Whether dependency arrows are drawn between chained rows.</summary>
    [Parameter]
    public bool ShowDependencies { get; set; } = true;

    /// <summary>
    /// Shows a placeholder chart instead of the rows, for the wait before any data has arrived.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="Refreshing"/> on purpose: these are the two states an API
    /// actually has. With nothing to show yet, a skeleton in the shape of the chart is better
    /// than an empty box; with rows already on screen, replacing them with a skeleton would
    /// throw away what the reader was reading.
    /// </remarks>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>
    /// Veils the chart while data is refreshed, leaving the rows already on screen visible
    /// underneath.
    /// </summary>
    [Parameter]
    public bool Refreshing { get; set; }

    /// <summary>What is being waited for, announced while loading or refreshing.</summary>
    [Parameter]
    public string LoadingText { get; set; } = "Loading tasks";

    /// <summary>How many placeholder rows the skeleton draws.</summary>
    [Parameter]
    public int LoadingRows { get; set; } = 6;

    /// <summary>Whether to scroll to today on the first render.</summary>
    [Parameter]
    public bool ScrollToTodayOnLoad { get; set; } = true;

    /// <summary>The base name for exported files, without an extension.</summary>
    [Parameter]
    public string ExportFileName { get; set; } = "gantt";

    /// <summary>Extra controls at the end of the toolbar.</summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    /// <summary>
    /// Lets bars be dragged to move them and their edges dragged to resize, snapped to the
    /// current zoom. Arrow keys do the same, so it works without a mouse.
    /// </summary>
    [Parameter]
    public bool Editable { get; set; }

    /// <summary>Whether dragging an edge can change a row's duration. Requires <see cref="Editable"/>.</summary>
    [Parameter]
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Raised after a row is moved or resized. The chart does not edit the list it was given -
    /// it reports the change and leaves the decision to the app.
    /// </summary>
    [Parameter]
    public EventCallback<VetoGanttChange> OnItemChanged { get; set; }

    /// <summary>
    /// Remembers this reader's edits in their own browser, under this key. Null stores nothing.
    /// </summary>
    /// <remarks>
    /// Local to one person and one browser. Anything that needs to outlive that, or be seen by
    /// anyone else, belongs on a server through <see cref="OnItemChanged"/>.
    /// </remarks>
    [Parameter]
    public string? PersistKey { get; set; }

    /// <summary>
    /// Where PGlite is loaded from. Null skips it and stores edits in <c>localStorage</c>
    /// instead, which is also what happens if the module cannot be fetched.
    /// </summary>
    [Parameter]
    public string? PersistenceModuleUrl { get; set; } = "https://cdn.jsdelivr.net/npm/@electric-sql/pglite/dist/index.js";

    /// <summary>Shows a control that clears everything stored about this reader for this chart.</summary>
    [Parameter]
    public bool ShowResetButton { get; set; } = true;

    /// <summary>The day treated as today.</summary>
    private DateOnly Today => TodayOverride ?? DateOnly.FromDateTime(DateTime.Now);

    private VetoGanttTimeline Timeline { get; set; } = default!;

    private IReadOnlyList<Row> Rows { get; set; } = [];

    /// <summary>The dependency arrows to draw, worked out from the visible rows.</summary>
    private IReadOnlyList<Link> Links { get; set; } = [];

    /// <summary>Every category present in the items, in first-seen order, with its colour.</summary>
    private IReadOnlyList<Category> Categories { get; set; } = [];

    private static readonly VetoGanttScale[] ScaleOptions =
        [VetoGanttScale.Day, VetoGanttScale.Week, VetoGanttScale.Month];

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();
        Rebuild();
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Scale.ToModifier()));

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-gantt-sidebar", SidebarWidth)
            .AddStyle("--veto-gantt-row-height", $"{RowHeight.ToString("0.##", CultureInfo.InvariantCulture)}px")
            .AddStyle("--veto-gantt-bar-color", BarColor);

    private string? FrameStyle => Height is null ? null : $"height: {Height}";

    private string? Sub ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    /// <summary>
    /// Varies the placeholder name widths so the skeleton reads as a list of tasks rather than
    /// a stack of identical bars.
    /// </summary>
    private static string SkeletonNameWidth ( int row )
        => (row % 3) switch { 0 => "62%", 1 => "78%", _ => "48%" };

    /// <summary>Varies the placeholder bar offsets and widths, for the same reason.</summary>
    private static string SkeletonBarStyle ( int row )
    {
        var left = 6 + (row % 4) * 13;
        var width = 18 + (row % 3) * 12;

        return string.Create(CultureInfo.InvariantCulture,
            $"margin-left: {left}%; width: {width}%");
    }

    /// <summary>
    /// Recomputes the visible rows and the axis. Runs on every parameter change because the
    /// filters, the scale and the items all feed the same result.
    /// </summary>
    private void Rebuild ()
    {
        // A category's colour is taken from the rows in it rather than configured separately,
        // so the filter doubles as a colour key and the two can never disagree.
        Categories = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Category))
            .GroupBy(i => i.Category!)
            .Select(g => new Category(
                g.Key,
                g.Select(i => i.Color).FirstOrDefault(c => c is not null) ?? BarColor))
            .ToList();

        var visible = Filter().Select(Resolve).ToList();

        Timeline = VetoGanttTimeline.Create(visible, Scale, Start, End, Today);
        Rows = Flatten(visible);
        Links = ShowDependencies ? BuildLinks() : [];
    }

    /// <summary>
    /// Works out one arrow per dependency between two rows that are both on screen.
    /// </summary>
    /// <remarks>
    /// A dependency whose predecessor is collapsed away, filtered out, or simply missing is
    /// skipped rather than drawn to nowhere - an arrow pointing at an invisible row is worse
    /// than no arrow.
    /// </remarks>
    private List<Link> BuildLinks ()
    {
        var index = new Dictionary<string, int>();

        for (var i = 0; i < Rows.Count; i++)
        {
            index[Rows[i].Item.Id] = i;
        }

        var links = new List<Link>();

        foreach (var row in Rows)
        {
            foreach (var predecessorId in row.Item.DependsOn)
            {
                if (predecessorId == row.Item.Id
                    || !index.TryGetValue(predecessorId, out var fromRow)
                    || !index.TryGetValue(row.Item.Id, out var toRow))
                {
                    continue;
                }

                var predecessor = Rows[fromRow].Item;

                links.Add(new Link(
                    predecessorId,
                    row.Item.Id,
                    Timeline.OffsetOf(predecessor.Start) + Timeline.WidthOf(predecessor.Start, predecessor.End),
                    fromRow,
                    Timeline.OffsetOf(row.Item.Start),
                    toRow));
            }
        }

        return links;
    }

    /// <summary>The height of the arrow layer: exactly the rows it spans.</summary>
    private double LinkLayerHeight => Rows.Count * RowHeight;

    /// <summary>
    /// Builds the elbow path for one arrow, in the layer's own coordinates.
    /// </summary>
    /// <remarks>
    /// When the successor starts well after the predecessor ends there is room to go out, across
    /// and in. When it does not - an overlap, or a dependency that is already violated - the
    /// path routes back between the two rows instead of drawing a line straight through them.
    /// </remarks>
    private string LinkPath ( Link link )
    {
        const double stub = 10;

        var y1 = link.FromRow * RowHeight + RowHeight / 2;
        var y2 = link.ToRow * RowHeight + RowHeight / 2;
        var x1 = link.FromX;
        var x2 = link.ToX;

        // Stops just short of the bar so the arrowhead sits against its edge, not inside it.
        var target = x2 - 2;

        if (target >= x1 + stub * 2)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"M {x1:0.##} {y1:0.##} H {x1 + stub:0.##} V {y2:0.##} H {target:0.##}");
        }

        var between = (y1 + y2) / 2;

        return string.Create(CultureInfo.InvariantCulture,
            $"M {x1:0.##} {y1:0.##} H {x1 + stub:0.##} V {between:0.##} H {target - stub:0.##} V {y2:0.##} H {target:0.##}");
    }

    /// <summary>Whether an arrow touches the selected row, so it can be picked out.</summary>
    private bool IsLinkActive ( Link link )
        => SelectedId is not null && (link.FromId == SelectedId || link.ToId == SelectedId);

    /// <summary>
    /// Applies the text and category filters, keeping any ancestor of a matching row so a task
    /// never appears detached from the project it belongs to.
    /// </summary>
    private List<VetoGanttItem> Filter ()
    {
        var byId = Items.ToDictionary(i => i.Id);

        bool Matches ( VetoGanttItem item )
        {
            if (!string.IsNullOrWhiteSpace(Search)
                && !item.Name.Contains(Search, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return item.Category is null || !_hiddenCategories.Contains(item.Category);
        }

        var keep = new HashSet<string>();

        foreach (var item in Items.Where(Matches))
        {
            keep.Add(item.Id);

            // Walk up so the matching row keeps its place in the tree.
            var parentId = item.ParentId;

            while (parentId is not null && byId.TryGetValue(parentId, out var parent))
            {
                if (!keep.Add(parent.Id))
                {
                    break;
                }

                parentId = parent.ParentId;
            }
        }

        return Items.Where(i => keep.Contains(i.Id)).ToList();
    }

    /// <summary>
    /// Orders rows within each level of the tree.
    /// </summary>
    /// <remarks>
    /// Sorting is applied per level rather than across the whole list, so a task never floats
    /// out from under the project it belongs to.
    /// </remarks>
    private IEnumerable<VetoGanttItem> Order ( IEnumerable<VetoGanttItem> items )
    {
        if (SortBy == VetoGanttSort.Source)
        {
            return SortDescending ? items.Reverse() : items;
        }

        var ordered = SortBy switch
        {
            VetoGanttSort.Name => items.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase),
            VetoGanttSort.Start => items.OrderBy(i => i.Start).ThenBy(i => i.Name),
            VetoGanttSort.End => items.OrderBy(i => i.End).ThenBy(i => i.Name),
            _ => items.OrderBy(i => i.DurationDays).ThenBy(i => i.Name)
        };

        return SortDescending ? ordered.Reverse() : ordered;
    }

    /// <summary>Turns the flat list into display order, skipping anything inside a collapsed group.</summary>
    private List<Row> Flatten ( IReadOnlyList<VetoGanttItem> items )
    {
        var children = items
            .Where(i => i.ParentId is not null)
            .GroupBy(i => i.ParentId!)
            .ToDictionary(g => g.Key, g => Order(g).ToList());

        var rows = new List<Row>();

        void Add ( VetoGanttItem item, int depth )
        {
            var hasChildren = children.ContainsKey(item.Id);
            rows.Add(new Row(item, depth, hasChildren));

            if (!hasChildren || !IsExpanded(item.Id))
            {
                return;
            }

            foreach (var child in children[item.Id])
            {
                Add(child, depth + 1);
            }
        }

        var roots = items.Where(i => i.ParentId is null || !items.Any(p => p.Id == i.ParentId));

        foreach (var root in Order(roots))
        {
            Add(root, 0);
        }

        return rows;
    }

    private bool IsExpanded ( string id ) => !_collapsed.Contains(id);

    private bool IsSelected ( VetoGanttItem item ) => SelectedId == item.Id;

    private bool IsCategoryOn ( string category ) => !_hiddenCategories.Contains(category);

    /// <summary>The inline style that gives a filter chip its category's colour.</summary>
    private static string CategoryStyle ( Category category )
        => $"--veto-gantt-chip-color: {category.Color}";

    private void ToggleGroup ( string id )
    {
        if (!_collapsed.Remove(id))
        {
            _collapsed.Add(id);
        }

        Rebuild();
    }

    private async Task ToggleCategoryAsync ( string category )
    {
        if (!_hiddenCategories.Remove(category))
        {
            _hiddenCategories.Add(category);
        }

        Rebuild();
        await Task.CompletedTask;
    }

    private async Task OnSearchChangedAsync ( string? value )
    {
        Search = value;
        Rebuild();
        await SearchChanged.InvokeAsync(value);
    }

    private async Task SetScaleAsync ( VetoGanttScale scale )
    {
        if (Scale == scale)
        {
            return;
        }

        Scale = scale;
        Rebuild();

        // The handler was given the old pixels-per-day and snap, so it is reattached with the
        // new ones on the next render.
        _dragAttached = false;

        await ScaleChanged.InvokeAsync(scale);

        // The axis has been redrawn at a different width, so the previous scroll position no
        // longer points anywhere meaningful.
        await ScrollToTodayAsync();
    }

    private async Task SelectAsync ( VetoGanttItem item )
    {
        SelectedId = item.Id;

        await SelectedIdChanged.InvokeAsync(item.Id);
        await OnItemSelected.InvokeAsync(item);
    }

    private string CellStyle ( VetoGanttTimeline.Column column )
        => string.Create(CultureInfo.InvariantCulture, $"left: {column.Offset:0.##}px; width: {column.Width:0.##}px");

    private string BarStyle ( VetoGanttItem item )
    {
        var offset = Timeline.OffsetOf(item.Start);
        var color = item.Color ?? BarColor;

        if (item.IsMilestone)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"left: {offset:0.##}px; --veto-gantt-item-color: {color}");
        }

        var width = Timeline.WidthOf(item.Start, item.End);

        return string.Create(CultureInfo.InvariantCulture,
            $"left: {offset:0.##}px; width: {width:0.##}px; --veto-gantt-item-color: {color}");
    }

    private string FormatRange ( VetoGanttItem item )
        => item.IsMilestone || item.Start == item.End
            ? item.Start.ToString("d MMM")
            : $"{item.Start:d MMM} – {item.End:d MMM}";

    private string Tooltip ( VetoGanttItem item )
    {
        var text = $"{item.Name} · {FormatRange(item)}";

        if (item.Progress is { } progress)
        {
            text += $" · {progress:P0}";
        }

        return item.Description is null ? text : $"{text} · {item.Description}";
    }

    /// <summary>A test-safe id fragment, since names may contain spaces and punctuation.</summary>
    private static string Slug ( string value )
        => new(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    // ------------------------------------------------------------------ Scrolling

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        if (firstRender)
        {
            await LoadOverridesAsync();

            if (ScrollToTodayOnLoad)
            {
                await ScrollToTodayAsync();
            }
        }

        await SyncDragHandlesAsync();
    }

    /// <summary>Brings today into view, or the start of the axis when today is outside it.</summary>
    private async Task ScrollToTodayAsync ()
    {
        var offset = Timeline.Contains(Today) ? Timeline.OffsetOf(Today) : 0;
        await InvokeModuleAsync("scrollTo", _scroller, offset);
    }

    /// <summary>Scrolls by a screenful in either direction.</summary>
    private Task ScrollByAsync ( double direction )
        => InvokeModuleAsync("scrollBy", _scroller, direction * 0.8);

    // ------------------------------------------------------------------ Export

    /// <summary>
    /// Exports the rows as currently filtered. The app gets first refusal through
    /// <see cref="OnExport"/>; if it does not take it, the file is downloaded here.
    /// </summary>
    private async Task ExportAsync ( VetoGanttExportFormat format )
    {
        var items = Rows.Select(r => r.Item).ToList();
        var request = new VetoGanttExportRequest(format, items, ExportFileName);

        if (OnExport.HasDelegate)
        {
            await OnExport.InvokeAsync(request);

            if (request.Handled)
            {
                return;
            }
        }

        var (extension, mime, content) = format == VetoGanttExportFormat.Csv
            ? ("csv", "text/csv", ToCsv(items))
            : ("json", "application/json", ToJson(items));

        await InvokeModuleAsync("download", $"{ExportFileName}.{extension}", mime, content);
    }

    /// <summary>Renders the rows as CSV.</summary>
    /// <remarks>
    /// Dates are written round-trippable (yyyy-MM-dd) rather than in the display format, so a
    /// spreadsheet in another locale reads them as the same days.
    /// </remarks>
    internal static string ToCsv ( IReadOnlyList<VetoGanttItem> items )
    {
        var csv = new StringBuilder();
        csv.AppendLine("Id,Name,Parent,Start,End,Days,Category,Progress,Milestone");

        foreach (var item in items)
        {
            csv.Append(Escape(item.Id)).Append(',')
               .Append(Escape(item.Name)).Append(',')
               .Append(Escape(item.ParentId)).Append(',')
               .Append(item.Start.ToString("yyyy-MM-dd")).Append(',')
               .Append(item.End.ToString("yyyy-MM-dd")).Append(',')
               .Append(item.DurationDays.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(Escape(item.Category)).Append(',')
               .Append(item.Progress?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
               .Append(item.IsMilestone ? "true" : "false")
               .AppendLine();
        }

        return csv.ToString();
    }

    /// <summary>Renders the rows as JSON.</summary>
    internal static string ToJson ( IReadOnlyList<VetoGanttItem> items )
        => JsonSerializer.Serialize(
            items.Select(i => new
            {
                i.Id,
                i.Name,
                Parent = i.ParentId,
                Start = i.Start.ToString("yyyy-MM-dd"),
                End = i.End.ToString("yyyy-MM-dd"),
                Days = i.DurationDays,
                i.Category,
                i.Progress,
                Milestone = i.IsMilestone
            }),
            new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Quotes a CSV field when it needs it. A field containing a comma, a quote or a newline
    /// would otherwise split the row in two when read back.
    /// </summary>
    private static string Escape ( string? value )
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.Contains(',')
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        return needsQuoting
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private async Task InvokeModuleAsync ( string function, params object?[] args )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-gantt.js");
            await _module.InvokeVoidAsync(function, args);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or a page that has gone away. The chart still renders; it just does
            // not scroll itself or produce a file.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        _selfRef?.Dispose();

        foreach (var module in new[] { _dragModule, _storeModule })
        {
            if (module is not null)
            {
                try
                {
                    await module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // The circuit or page is already gone.
                }
            }
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit or page is already gone.
            }
        }
    }

    private sealed record Row ( VetoGanttItem Item, int Depth, bool HasChildren );

    /// <summary>A category and the colour the rows in it are drawn with.</summary>
    private sealed record Category ( string Name, string Color );

    // ------------------------------------------------------------------ Editing

    /// <summary>
    /// The dates a row is drawn at: this reader's saved edit if there is one, otherwise what the
    /// app supplied. Applied at render time rather than by rewriting the list, which the chart
    /// does not own.
    /// </summary>
    private VetoGanttItem Resolve ( VetoGanttItem item )
        => _overrides.TryGetValue(item.Id, out var dates)
            ? item with { Start = dates.Start, End = dates.End }
            : item;

    /// <summary>
    /// Called from JavaScript when a drag finishes, with how many whole days it moved.
    /// Public because JS interop requires it, not because apps should call it.
    /// </summary>
    /// <param name="id">The row that was dragged.</param>
    /// <param name="mode">move, start or end.</param>
    /// <param name="days">Days moved, already snapped to the current zoom.</param>
    [JSInvokable]
    public async Task OnBarDragged ( string id, string mode, int days )
    {
        var edit = mode switch
        {
            "start" => VetoGanttEdit.StartChanged,
            "end" => VetoGanttEdit.EndChanged,
            _ => VetoGanttEdit.Moved
        };

        await ApplyEditAsync(id, edit, days);
    }

    /// <summary>Applies a move or resize, whether it came from a drag or the keyboard.</summary>
    private async Task ApplyEditAsync ( string id, VetoGanttEdit edit, int days )
    {
        var item = Items.FirstOrDefault(i => i.Id == id);

        if (item is null || days == 0 || !Editable)
        {
            return;
        }

        var current = Resolve(item);
        var start = current.Start;
        var end = current.End;

        switch (edit)
        {
            case VetoGanttEdit.Moved:
                start = start.AddDays(days);
                end = end.AddDays(days);
                break;

            case VetoGanttEdit.StartChanged:
                start = start.AddDays(days);

                // A bar cannot be dragged inside out; the start stops at the end.
                if (start > end)
                {
                    start = end;
                }

                break;

            case VetoGanttEdit.EndChanged:
                end = end.AddDays(days);

                if (end < start)
                {
                    end = start;
                }

                break;
        }

        if (start == current.Start && end == current.End)
        {
            return;
        }

        _overrides[id] = (start, end);
        Rebuild();
        StateHasChanged();

        await PersistAsync(id, start, end);

        await OnItemChanged.InvokeAsync(new VetoGanttChange
        {
            Id = id,
            Edit = edit,
            PreviousStart = current.Start,
            PreviousEnd = current.End,
            Start = start,
            End = end,
            Days = days
        });
    }

    /// <summary>
    /// Moves or resizes the focused row from the keyboard: arrows move, Shift+arrows change the
    /// end. Without this the chart would be editable only with a mouse.
    /// </summary>
    private async Task OnBarKeyDownAsync ( KeyboardEventArgs args, VetoGanttItem item )
    {
        if (!Editable || args.Key is not ("ArrowLeft" or "ArrowRight"))
        {
            return;
        }

        var step = Scale.SnapDays() * (args.Key == "ArrowRight" ? 1 : -1);
        var edit = args.ShiftKey && Resizable ? VetoGanttEdit.EndChanged : VetoGanttEdit.Moved;

        await ApplyEditAsync(item.Id, edit, step);
    }

    // ------------------------------------------------------------------ Persistence

    /// <summary>Loads this reader's saved edits, if the chart was given a key.</summary>
    private async Task LoadOverridesAsync ()
    {
        if (PersistKey is null || _storeOpened)
        {
            return;
        }

        _storeOpened = true;

        try
        {
            _storeModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-gantt-store.js");
            _storeBackend = await _storeModule.InvokeAsync<string>("open", PersistenceModuleUrl);

            var saved = await _storeModule.InvokeAsync<StoredOverride[]>("load", PersistKey);

            foreach (var row in saved)
            {
                if (DateOnly.TryParse(row.Start, CultureInfo.InvariantCulture, out var start)
                    && DateOnly.TryParse(row.Finish, CultureInfo.InvariantCulture, out var finish))
                {
                    _overrides[row.Item] = (start, finish);
                }
            }

            Rebuild();
            StateHasChanged();
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or storage is unavailable. The chart works; edits just do not persist.
            _storeBackend = "unavailable";
            StateHasChanged();
        }
    }

    private async Task PersistAsync ( string id, DateOnly start, DateOnly end )
    {
        if (PersistKey is null || _storeModule is null)
        {
            return;
        }

        try
        {
            await _storeModule.InvokeVoidAsync("save", PersistKey, id,
                start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // The edit still shows; it just will not survive a reload.
        }
    }

    /// <summary>
    /// Clears everything stored about this reader for this chart and puts every row back to the
    /// dates the app supplied.
    /// </summary>
    public async Task ResetAsync ()
    {
        _overrides.Clear();
        Rebuild();
        StateHasChanged();

        if (PersistKey is null || _storeModule is null)
        {
            return;
        }

        try
        {
            await _storeModule.InvokeVoidAsync("clear", PersistKey);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Already gone, or the page is closing.
        }
    }

    /// <summary>
    /// Whether a row can be dragged. A summary row cannot: its dates are the span of its
    /// children, so moving it would say something the children do not agree with.
    /// </summary>
    private bool IsEditable ( Row row ) => Editable && !row.HasChildren && !row.Item.IsMilestone;

    /// <summary>
    /// Attaches the delegated drag handler to the grid. Re-attaching updates the measurements,
    /// which is how a change of scale takes effect.
    /// </summary>
    private async Task SyncDragHandlesAsync ()
    {
        if (!Editable)
        {
            return;
        }

        try
        {
            _dragModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-gantt-drag.js");
            _selfRef ??= DotNetObjectReference.Create(this);

            await _dragModule.InvokeVoidAsync("attach", _grid, _selfRef, new
            {
                pixelsPerDay = Timeline.PixelsPerDay,
                snapDays = Scale.SnapDays(),
                resizable = Resizable
            });

            if (!_dragAttached)
            {
                _dragAttached = true;

                // Re-rendered so data-draggable stops saying false. Attaching happens after a
                // render, so without this the element would advertise the state it had before
                // the handler existed - and anything waiting on that signal, an app or a test,
                // would wait forever. The next render finds _dragAttached already true and
                // stops here, so this does not loop.
                StateHasChanged();
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or the page has gone. Bars stay read-only, and the keyboard path
            // still works because it does not touch JavaScript.
        }
    }

    /// <summary>
    /// Which storage the edits ended up in - <c>pglite</c>, <c>localstorage</c> or
    /// <c>memory</c>, <c>none</c> before the store has opened, and <c>unavailable</c> if it
    /// could not be opened at all. Surfaced on the element so an app can show where a reader's
    /// edits are kept, and so anything waiting for storage to be ready has something to wait on.
    /// </summary>
    public string StoreBackend => _storeBackend;

    /// <summary>Whether the delegated drag handler is attached.</summary>
    private bool DragHandleAttached => _dragAttached;


    /// <summary>True when this reader has edits that a reset would clear.</summary>
    private bool HasEdits => _overrides.Count > 0;

    /// <summary>One saved override, as it comes back from the store.</summary>
    private sealed record StoredOverride ( string Item, string Start, string Finish );

    /// <summary>One dependency arrow, resolved to row positions and pixel offsets.</summary>
    private sealed record Link ( string FromId, string ToId, double FromX, int FromRow, double ToX, int ToRow );
}
