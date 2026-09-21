using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Veto.Components.Menu;
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
    // Folding is remembered both ways, because the two layouts start from opposite defaults:
    // rows open, inline folded. Whichever the layout, an explicit choice is kept in both.
    private readonly HashSet<string> _collapsed = [];
    private readonly HashSet<string> _expanded = [];
    private readonly HashSet<string> _hiddenCategories = [];
    private readonly HashSet<string> _activeFilters = [];

    // The last values the parent passed for the two bindable filter sets. A parent re-render
    // hands the same values back; only a change is applied, so a click the parent has not bound
    // is not undone by an unrelated render.
    private IReadOnlyCollection<string>? _hiddenCategoriesParameter;
    private IReadOnlyCollection<string>? _activeFiltersParameter;

    private readonly Dictionary<string, (DateOnly Start, DateOnly End)> _overrides = [];

    // How many days Earlier and Later have moved the range from the one the parent set, and the
    // parameters that range came from - a change to them starts paging again from nothing.
    private int _pagedDays;
    private (DateOnly?, DateOnly?, VetoGanttView)? _rangeParameters;

    // Which edge of the timeline to scroll to once a paged range is drawn: -1 the start, 1 the end.
    private int _pagedScrollPending;

    // The Today button's scroll, waiting for the range it moved back to be drawn.
    private bool _centreNowPending;

    // The right-clicked item and where the pointer was; the menu takes focus once it is drawn.
    private (VetoGanttItem Item, double X, double Y)? _contextMenu;
    private ElementReference _contextMenuPanel;
    private bool _contextMenuOpening;

    private IJSObjectReference? _module;
    private IJSObjectReference? _dragModule;
    private IJSObjectReference? _storeModule;
    private DotNetObjectReference<VetoGantt>? _selfRef;
    private ElementReference _scroller;
    private ElementReference _grid;
    private bool _dragAttached;
    private (string? Grid, double PixelsPerDay, int SnapDays, bool Resizable) _dragAttachment;
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
    /// A menu for the item under a right-click - on its bar or its name - opened at the pointer.
    /// Fill it with <c>VetoMenuItem</c>s: each does what its <c>OnClick</c> says, and choosing one
    /// closes the menu. Null leaves right-click to the browser.
    /// </summary>
    [Parameter]
    public RenderFragment<VetoGanttItem>? ItemContextMenu { get; set; }

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

    /// <summary>
    /// Shows today's date on a line of its own above the timeline. It sits outside the part
    /// that scrolls, so the date stays in view however far the timeline is scrolled.
    /// </summary>
    [Parameter]
    public bool ShowDateLine { get; set; } = true;

    /// <summary>The format of the date line - any .NET date format, such as <c>"ddd d MMM"</c>.</summary>
    [Parameter]
    public string DateLineFormat { get; set; } = "dddd d MMMM yyyy";

    /// <summary>
    /// The date line drawn your own way, given today's date. Replaces the formatted text;
    /// <see cref="ShowDateLine"/> still decides whether the line is there at all.
    /// </summary>
    [Parameter]
    public RenderFragment<DateOnly>? DateLineTemplate { get; set; }

    /// <summary>
    /// The format of the header's lower row - the hours, days, weeks or months. Null keeps each
    /// scale's own: <c>HH:00</c>, the day of the month, <c>MMM</c>.
    /// </summary>
    [Parameter]
    public string? HeaderFormat { get; set; }

    /// <summary>
    /// The format of the header's upper row - the day above the hours, the month above the
    /// days, the year above the months. Null keeps each scale's own.
    /// </summary>
    [Parameter]
    public string? HeaderGroupFormat { get; set; }

    /// <summary>The text shown in the empty search box.</summary>
    [Parameter]
    public string SearchPlaceholder { get; set; } = "Filter tasks";

    /// <summary>The label of the view switch's today option.</summary>
    [Parameter]
    public string TodayViewLabel { get; set; } = "Today";

    /// <summary>The label of the view switch's schedule option.</summary>
    [Parameter]
    public string ScheduleViewLabel { get; set; } = "Schedule";

    /// <summary>The text of the button that scrolls back to now.</summary>
    [Parameter]
    public string TodayButtonText { get; set; } = "Today";

    /// <summary>The text of the export menu's trigger, and its tooltip.</summary>
    [Parameter]
    public string ExportText { get; set; } = "Export";

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

    /// <summary>
    /// The categories switched off. Supports <c>@bind-HiddenCategories</c>, so the page can set
    /// the category filter itself and hear about every click on it. Null leaves it to the chart.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<string>? HiddenCategories { get; set; }

    /// <summary>Raised when a category is switched on or off.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> HiddenCategoriesChanged { get; set; }

    /// <summary>
    /// Filters of the app's own, each a toggle in the toolbar. While one is on, only the rows its
    /// predicate keeps are listed; several on at once must all keep a row.
    /// </summary>
    [Parameter]
    public IReadOnlyList<VetoGanttFilter> Filters { get; set; } = [];

    /// <summary>The ids of the <see cref="Filters"/> that are on. Supports <c>@bind-ActiveFilters</c>.</summary>
    [Parameter]
    public IReadOnlyCollection<string>? ActiveFilters { get; set; }

    /// <summary>Raised when one of the <see cref="Filters"/> is switched on or off.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ActiveFiltersChanged { get; set; }

    /// <summary>Whether the toolbar shows a toggle for each of the <see cref="Filters"/>.</summary>
    [Parameter]
    public bool ShowFilters { get; set; } = true;

    /// <summary>
    /// A filter with no toggle: rows it rejects are never listed. For a rule the page applies on
    /// its own terms - a permission, a selected team - rather than one the reader switches.
    /// </summary>
    [Parameter]
    public Func<VetoGanttItem, bool>? ItemFilter { get; set; }

    /// <summary>
    /// Raised after any filter changes - the text, a category, one of the <see cref="Filters"/> -
    /// with all of them and the rows they leave, so a page can react to one event rather than three.
    /// </summary>
    [Parameter]
    public EventCallback<VetoGanttFilterState> OnFilterChanged { get; set; }

    /// <summary>
    /// The zoom levels the scale picker offers, in order. <c>[VetoGanttScale.Day]</c> offers day
    /// alone - and with one option there is nothing to pick, so the picker is not shown at all.
    /// </summary>
    [Parameter]
    public IReadOnlyList<VetoGanttScale> Scales { get; set; } = [VetoGanttScale.QuarterHour, VetoGanttScale.Hour, VetoGanttScale.Day, VetoGanttScale.Week, VetoGanttScale.Month];

    /// <summary>Whether the toolbar includes the Day / Week / Month picker.</summary>
    [Parameter]
    public bool ShowScalePicker { get; set; } = true;

    /// <summary>Whether the toolbar includes the Today and earlier/later buttons.</summary>
    [Parameter]
    public bool ShowNavigation { get; set; } = true;

    /// <summary>
    /// What Earlier and Later do once the timeline cannot scroll any further that way - always,
    /// on a range fitted to the width. On, the range itself moves: a day at a time on the today
    /// view, and by a step of the scale otherwise (never more than the range is long, so no day
    /// is skipped). Off, the buttons only scroll, and do nothing at either end.
    /// </summary>
    [Parameter]
    public bool PageBeyondRange { get; set; } = true;

    /// <summary>
    /// Raised when Earlier or Later moves the range, with the days now on the axis - for a page
    /// that loads its rows a range at a time. The Today button, which moves it back, raises it too.
    /// </summary>
    [Parameter]
    public EventCallback<VetoGanttRange> OnRangeChanged { get; set; }

    /// <summary>Whether the toolbar includes the export menu.</summary>
    [Parameter]
    public bool Exportable { get; set; } = true;

    /// <summary>
    /// The formats the export menu offers, in order. Leave one out to disable it; an empty list
    /// hides the menu, as <see cref="Exportable"/> = false does.
    /// </summary>
    [Parameter]
    public IReadOnlyList<VetoGanttExportFormat> ExportFormats { get; set; } = [VetoGanttExportFormat.Csv, VetoGanttExportFormat.Json];

    /// <summary>
    /// What opens the export menu. Hover by default: export is the toolbar's least-used
    /// control, so it costs one button of room and shows its formats when the reader goes
    /// looking. A click opens it as well, and so does the keyboard.
    /// </summary>
    [Parameter]
    public VetoMenuTrigger ExportOpenOn { get; set; } = VetoMenuTrigger.Hover;

    /// <summary>
    /// How much of each export format is drawn: the icon and its name, the icon alone, or the
    /// name alone. Icon-only suits a crowded toolbar - the names stay as the items' accessible
    /// names and tooltips either way.
    /// </summary>
    [Parameter]
    public VetoMenuAppearance ExportAppearance { get; set; } = VetoMenuAppearance.IconAndText;

    /// <summary>
    /// Whether the export trigger shows the word "Export" beside its icon. On by default; off
    /// leaves a single square button, which is what a narrow toolbar wants.
    /// </summary>
    [Parameter]
    public bool ShowExportLabel { get; set; } = true;

    /// <summary>
    /// Which corner of the trigger the export menu hangs from. The trailing edge by default,
    /// because the export control sits at the end of the toolbar and a menu aligned to its
    /// leading edge would hang off the chart.
    /// </summary>
    [Parameter]
    public VetoMenuPlacement ExportPlacement { get; set; } = VetoMenuPlacement.BottomEnd;

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
    /// Whether hovering or focusing a bar shows a tooltip. Turning it off leaves each bar's
    /// accessible name in place, so a screen reader still hears the row's name and dates.
    /// </summary>
    [Parameter]
    public bool ShowTooltips { get; set; } = true;

    /// <summary>
    /// Replaces the text of the browser's native tooltip - by default the name, the dates, the
    /// progress and the description. Return null to give that row no tooltip at all.
    /// </summary>
    [Parameter]
    public Func<VetoGanttItem, string?>? TooltipText { get; set; }

    /// <summary>
    /// A rich hover card, drawn in place of the browser's native tooltip. It opens below the bar,
    /// or above it for rows in the lower half of the chart, when the bar is hovered or focused.
    /// Takes precedence over <see cref="TooltipText"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<VetoGanttItem>? TooltipTemplate { get; set; }

    /// <summary>
    /// How high the <see cref="TooltipTemplate"/> card layers. Null uses the library's
    /// <c>--veto-z-tooltip</c> token, 1000 - above page content and sticky bars. The card is
    /// placed against the window rather than inside the timeline, so the chart's own scrolling
    /// never cuts it off; this decides what it may still be covered by.
    /// </summary>
    [Parameter]
    public int? TooltipZIndex { get; set; }

    /// <summary>
    /// What span the chart shows. <see cref="VetoGanttView.Today"/> by default: today alone,
    /// fitted to the width, listing only the rows with work today. Supports <c>@bind-View</c>.
    /// An explicit <see cref="Start"/> and <see cref="End"/> set the axis in either view.
    /// </summary>
    [Parameter]
    public VetoGanttView View { get; set; } = VetoGanttView.Today;

    /// <summary>Raised when the view changes.</summary>
    [Parameter]
    public EventCallback<VetoGanttView> ViewChanged { get; set; }

    /// <summary>Whether the toolbar includes the Today / Schedule switch.</summary>
    [Parameter]
    public bool ShowViewPicker { get; set; } = true;

    /// <summary>
    /// Stretches a short axis across the full width of the chart, rather than drawing it at the
    /// scale's own density and leaving the rest empty. A range longer than the chart is never
    /// squeezed - it keeps its density and scrolls.
    /// </summary>
    [Parameter]
    public bool FitToWidth { get; set; } = true;

    /// <summary>
    /// How a parent's sub-items are laid out: a row each (<see cref="VetoGanttSubItemLayout.Rows"/>),
    /// or side by side on the parent's own line (<see cref="VetoGanttSubItemLayout.Inline"/>) -
    /// many jobs on one row.
    /// </summary>
    [Parameter]
    public VetoGanttSubItemLayout SubItemLayout { get; set; } = VetoGanttSubItemLayout.Rows;

    /// <summary>
    /// Whether parents can be folded and unfolded. Off, the fold arrows go: rows stay open, and
    /// an inline parent keeps its sub-items on its own line.
    /// </summary>
    [Parameter]
    public bool Collapsible { get; set; } = true;

    /// <summary>
    /// Trims the start of the axis to a time of day - with <see cref="EndTime"/>, a window such
    /// as 14:00 to 20:00, which is then stretched across the chart. Null starts at midnight.
    /// </summary>
    [Parameter]
    public TimeOnly? StartTime { get; set; }

    /// <summary>Trims the end of the axis to a time of day. Null runs to the end of the last day.</summary>
    [Parameter]
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// The scale the today view is drawn at. Hours by default: one day at a day scale would be a
    /// single column saying nothing about when in the day anything happens.
    /// </summary>
    [Parameter]
    public VetoGanttScale TodayScale { get; set; } = VetoGanttScale.Hour;

    /// <summary>
    /// The time of day treated as now, for the marker at the hour scale. Defaults to the clock;
    /// set it, with <see cref="TodayOverride"/>, to keep a screenshot or a test stable.
    /// </summary>
    [Parameter]
    public TimeOnly? TimeOverride { get; set; }

    /// <summary>
    /// Shows the time on top of the now marker, in the header - so it stays in view when the
    /// rows are scrolled.
    /// </summary>
    [Parameter]
    public bool ShowNowLabel { get; set; } = true;

    /// <summary>
    /// The format of the time on the now marker. Null reads <c>HH:mm</c> on the hour scales and
    /// the date on the coarser ones, where the marker stands for the whole of today.
    /// </summary>
    [Parameter]
    public string? NowLabelFormat { get; set; }

    /// <summary>
    /// The height of the band at the foot of the header that the time on the now marker sits
    /// in, as a CSS length. The band keeps the time clear of the hour and day labels.
    /// </summary>
    [Parameter]
    public string NowBandHeight { get; set; } = "1rem";

    /// <summary>How the time on the now marker is drawn: a coloured tab, or the time alone.</summary>
    [Parameter]
    public VetoGanttNowLabelAppearance NowLabelAppearance { get; set; } = VetoGanttNowLabelAppearance.Tab;

    /// <summary>
    /// Pins the chart to the light or dark palette, whatever the page is in. Inherit, the
    /// default, follows the page - so a theme toggle anywhere on it switches the chart too.
    /// </summary>
    [Parameter]
    public VetoColorScheme ColorScheme { get; set; } = VetoColorScheme.Inherit;

    /// <summary>
    /// Moves the now marker, and its time, as the clock moves - checked twice a minute. Has no
    /// effect with <see cref="TimeOverride"/> set, which holds now still.
    /// </summary>
    [Parameter]
    public bool LiveNowMarker { get; set; } = true;

    /// <summary>
    /// The narrowest an hour is drawn, in pixels. Fitting a whole day into a narrow chart stops
    /// here: past it, the hours keep a width their "14:00" labels fit in, and the timeline
    /// scrolls instead.
    /// </summary>
    [Parameter]
    public double MinHourWidth { get; set; } = 44;

    /// <summary>
    /// Stacks jobs that overlap in time into lanes when they share a line, growing the row a
    /// lane at a time, so two jobs running at once are both visible. Off, they are drawn over
    /// each other at the row's own height.
    /// </summary>
    [Parameter]
    public bool StackOverlaps { get; set; } = true;

    /// <summary>
    /// Groups the top-level rows by a key - a server, a project, a team - under a row per key,
    /// without the app writing those rows itself. A group spans its jobs, folds like any parent,
    /// and with <see cref="SubItemLayout"/> set to inline keeps its jobs on one line. Rows whose
    /// key is null are left ungrouped.
    /// </summary>
    /// <example>
    /// <code>GroupBy="item =&gt; item.Category"</code>
    /// </example>
    [Parameter]
    public Func<VetoGanttItem, string?>? GroupBy { get; set; }

    /// <summary>
    /// The height of the rows <see cref="GroupBy"/> makes, in pixels - they are the chart's
    /// own rows, so this is where their height is set. Null takes <see cref="RowHeight"/>.
    /// </summary>
    [Parameter]
    public double? GroupRowHeight { get; set; }

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

    /// <summary>
    /// Whether the timeline scrolls the now marker to its centre by itself: on load, and again
    /// whenever the view or the scale changes. On the hour scales that is the current time; on the coarser
    /// ones, today. Off, the timeline opens at the start of its axis.
    /// </summary>
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

    /// <summary>
    /// The day the date line names: the day on the axis in the today view - today, until Earlier
    /// or Later moves it - and today itself on the schedule.
    /// </summary>
    private DateOnly ShownDay => View == VetoGanttView.Today ? Timeline.Start : Today;

    /// <summary>The moment treated as now: today, at the overridden time or the clock's.</summary>
    private DateTime Now => Today.ToDateTime(TimeOverride ?? TimeOnly.FromDateTime(DateTime.Now));

    /// <summary>The time on top of the now marker.</summary>
    private string NowLabel => EffectiveScale.ShowsClockTime()
        ? Now.ToString(NowLabelFormat ?? "HH:mm")
        : Today.ToString(NowLabelFormat ?? "d MMM");

    private Timer? _clock;

    /// <summary>
    /// Keeps the now marker on the clock. Only redraws when the minute has changed, so a chart
    /// nobody is looking at costs a comparison every thirty seconds.
    /// </summary>
    private void StartClock ()
    {
        if (!LiveNowMarker || _clock is not null)
        {
            return;
        }

        var shown = DateTime.Now.ToString("yyyyMMddHHmm");
        var day = DateOnly.FromDateTime(DateTime.Now);

        _clock = new Timer(_ =>
        {
            var current = DateTime.Now.ToString("yyyyMMddHHmm");

            if (current == shown || TimeOverride is not null || !ShowToday)
            {
                return;
            }

            shown = current;

            // Past midnight the axis itself is out of date - the today view is still drawing
            // yesterday - so it is rebuilt, not just redrawn.
            var today = DateOnly.FromDateTime(DateTime.Now);
            var newDay = today != day && TodayOverride is null;
            day = today;

            _ = InvokeAsync(() =>
            {
                if (newDay)
                {
                    Rebuild();
                }

                StateHasChanged();
            });
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>The scale actually drawn: the today view has its own, so the picker's choice survives it.</summary>
    private VetoGanttScale EffectiveScale => View == VetoGanttView.Today ? TodayScale : Scale;

    /// <summary>
    /// Where the today marker goes: at now on the hour scale, where the time of day is visible,
    /// and at the start of today on the coarser ones. Null when that is off the axis.
    /// </summary>
    private double? TodayMarkerOffset => EffectiveScale.ShowsClockTime()
        ? (Timeline.Contains(Now) ? Timeline.OffsetAt(Now) : null)
        : (Timeline.Contains(Today) ? Math.Max(0, Timeline.OffsetOf(Today)) : null);

    private VetoGanttTimeline Timeline { get; set; } = default!;

    /// <summary>The items plus any rows <see cref="GroupBy"/> makes. What every lookup works from.</summary>
    private IReadOnlyList<VetoGanttItem> AllItems { get; set; } = [];

    // The ids of the rows GroupBy made, which are never matched by a filter in their own right:
    // a group is listed only for the jobs under it.
    private readonly HashSet<string> _groupIds = [];

    // Where each row starts, top to bottom. A row with stacked lanes is taller than the rest, so
    // a row's position is a running total rather than its index times the row height.
    private double[] _rowTops = [];

    private IReadOnlyList<Row> Rows { get; set; } = [];

    /// <summary>The dependency arrows to draw, worked out from the visible rows.</summary>
    private IReadOnlyList<Link> Links { get; set; } = [];

    /// <summary>Every category present in the items, in first-seen order, with its colour.</summary>
    private IReadOnlyList<Category> Categories { get; set; } = [];

    private static readonly VetoGanttView[] ViewOptions = [VetoGanttView.Today, VetoGanttView.Schedule];

    private string ViewLabel ( VetoGanttView view ) => view == VetoGanttView.Today ? TodayViewLabel : ScheduleViewLabel;


    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        ApplyBoundSet(HiddenCategories, ref _hiddenCategoriesParameter, _hiddenCategories);
        ApplyBoundSet(ActiveFilters, ref _activeFiltersParameter, _activeFilters);

        // A new range from the parent starts from where it says, not from where paging had got to.
        if ((Start, End, View) != _rangeParameters)
        {
            _rangeParameters = (Start, End, View);
            _pagedDays = 0;
        }

        Rebuild();
    }

    /// <summary>Takes a bound set from the parent when it changed since the last one it passed.</summary>
    private static void ApplyBoundSet ( IReadOnlyCollection<string>? incoming, ref IReadOnlyCollection<string>? last, HashSet<string> into )
    {
        if (incoming is null)
        {
            last = null;
            return;
        }

        if (last is not null && last.Count == incoming.Count && last.All(incoming.Contains))
        {
            return;
        }

        last = incoming.ToArray();
        into.Clear();
        into.UnionWith(incoming);
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(EffectiveScale.ToModifier()))
            .AddClass(Modifier("single-header"), SingleHeaderRow)
            .AddClass(Modifier("now-label"), ShowToday && ShowNowLabel);

    /// <summary>
    /// Which way the time tab hangs from the marker: centred, or held inside the timeline when
    /// the marker is so close to an edge that a centred tab would be cut in half.
    /// </summary>
    private string NowLabelClass ( double offset )
    {
        const double reach = 24;

        var edge = offset < reach ? " veto-gantt__now-label--start"
            : offset > Timeline.Width - reach ? " veto-gantt__now-label--end"
            : string.Empty;
        var appearance = NowLabelAppearance == VetoGanttNowLabelAppearance.Text ? " veto-gantt__now-label--text" : string.Empty;

        return "veto-gantt__now-label" + edge + appearance;
    }

    /// <summary>
    /// Whether the header's upper row is left out. A single day in hours would only repeat, above
    /// the hours, the date the date line already gives - so with the date line showing, it goes.
    /// A range of days keeps it: there it is what says which day each hour belongs to.
    /// </summary>
    private bool SingleHeaderRow =>
        ShowDateLine && EffectiveScale == VetoGanttScale.Hour && Timeline is not null && Timeline.Start == Timeline.End;

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-gantt-sidebar", SidebarWidth)
            .AddStyle("--veto-gantt-row-height", $"{RowHeight.ToString("0.##", CultureInfo.InvariantCulture)}px")
            .AddStyle("--veto-gantt-bar-color", BarColor)
            .AddStyle("--veto-gantt-now-band", NowBandHeight, ShowToday && ShowNowLabel);

    private string? FrameStyle => Height is null ? null : $"height: {Height}";

    private string? Sub ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    /// <summary>
    /// Varies the placeholder name widths so the skeleton reads as a list of tasks rather than
    /// a stack of identical bars.
    /// </summary>
    private static string SkeletonNameWidth ( int row ) =>
        (row % 3) switch { 0 => "62%", 1 => "78%", _ => "48%" };

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

        AllItems = WithGroups();

        var visible = Filter().Select(Resolve).ToList();

        var previous = Timeline?.PixelsPerDay;
        Timeline = CreateTimeline(visible);

        // The drag handler converts pixels to days, so a new density needs it re-attached.
        if (previous is not null && previous != Timeline.PixelsPerDay)
        {
            _dragAttached = false;
        }
        Rows = Flatten(visible);

        _rowTops = new double[Rows.Count];
        var top = 0.0;

        for (var i = 0; i < Rows.Count; i++)
        {
            _rowTops[i] = top;
            top += LineHeight(Rows[i]) * Rows[i].Lanes;
        }

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
        // An inline sub-item sits on its parent's line, in its own lane, so arrows to and from it
        // start and end there.
        Dictionary<string, (VetoGanttItem Item, double Y)> index = new();

        for (var i = 0; i < Rows.Count; i++)
        {
            index[Rows[i].Item.Id] = (Rows[i].Item, LaneCentre(i, 0));

            foreach (var inline in Rows[i].Inline)
            {
                index[inline.Id] = (inline, LaneCentre(i, Rows[i].LaneOf(inline)));
            }
        }

        var links = new List<Link>();

        foreach (var (item, toY) in index.Values)
        {
            foreach (var predecessorId in item.DependsOn)
            {
                if (predecessorId == item.Id || !index.TryGetValue(predecessorId, out var from))
                {
                    continue;
                }

                var (fromLeft, fromWidth) = Timeline.Place(from.Item.StartsAt, from.Item.EndsAt);
                var (toLeft, _) = Timeline.Place(item.StartsAt, item.EndsAt);

                links.Add(new Link(predecessorId, item.Id, fromLeft + fromWidth, from.Y, toLeft, toY));
            }
        }

        return links;
    }

    /// <summary>The height of the arrow layer: exactly the rows it spans.</summary>
    private double LinkLayerHeight => Rows.Count == 0 ? 0 : _rowTops[^1] + LineHeight(Rows[^1]) * Rows[^1].Lanes;

    /// <summary>The height of one lane of a row: its item's own, or the chart's.</summary>
    private double LineHeight ( Row row ) => row.Item.RowHeight ?? RowHeight;

    /// <summary>
    /// A row's own height, when it is not the chart's: a height of its own sets the variable
    /// its bar is sized from, and stacked lanes multiply it.
    /// </summary>
    private string? RowHeightStyle ( Row row )
    {
        if (row.Item.RowHeight is null && row.Lanes == 1)
        {
            return null;
        }

        var line = LineHeight(row);

        return string.Create(CultureInfo.InvariantCulture,
            $"--veto-gantt-row-height: {line:0.##}px; height: {line * row.Lanes:0.##}px");
    }

    /// <summary>The vertical centre of a lane within the chart, for the arrows.</summary>
    private double LaneCentre ( int row, int lane )
        => _rowTops[row] + (lane + 0.5) * LineHeight(Rows[row]);

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

        var y1 = link.FromY;
        var y2 = link.ToY;
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
    /// The items with a row added per <see cref="GroupBy"/> key, each group placed where its
    /// first job was, and its jobs moved under it.
    /// </summary>
    private IReadOnlyList<VetoGanttItem> WithGroups ()
    {
        _groupIds.Clear();

        if (GroupBy is null)
        {
            return Items;
        }

        var ids = Items.Select(i => i.Id).ToHashSet();
        var members = new Dictionary<string, List<VetoGanttItem>>();

        string? KeyOf ( VetoGanttItem item )
            => item.ParentId is null || !ids.Contains(item.ParentId) ? GroupBy(item) : null;

        foreach (var item in Items)
        {
            if (KeyOf(item) is { } key)
            {
                if (!members.TryGetValue(key, out var list))
                {
                    members[key] = list = [];
                }

                list.Add(item with { ParentId = GroupId(key) });
            }
        }

        var result = new List<VetoGanttItem>(Items.Count + members.Count);
        var emitted = new HashSet<string>();

        foreach (var item in Items)
        {
            if (KeyOf(item) is not { } key)
            {
                result.Add(item);
                continue;
            }

            if (!emitted.Add(key))
            {
                continue;
            }

            var jobs = members[key];
            var group = new VetoGanttItem(
                GroupId(key),
                key,
                jobs.Min(job => Resolve(job).StartsAt),
                jobs.Max(job => Resolve(job).EndsAt))
            {
                RowHeight = GroupRowHeight
            };

            _groupIds.Add(group.Id);
            result.Add(group);
            result.AddRange(jobs);
        }

        return result;
    }

    /// <summary>A group row's id: readable in a test id, and apart from any id an app would use.</summary>
    private static string GroupId ( string key ) => $"group-{Slug(key)}";

    /// <summary>
    /// Puts overlapping jobs on separate lanes: each job takes the first lane that is free by
    /// the time it starts. Jobs that only touch - one ending at 09:00, the next starting then -
    /// share a lane.
    /// </summary>
    private static Dictionary<string, int> AssignLanes ( IEnumerable<VetoGanttItem> jobs, out int lanes )
    {
        var lane = new Dictionary<string, int>();
        var freeAt = new List<DateTime>();

        foreach (var job in jobs.OrderBy(j => j.StartsAt).ThenBy(j => j.EndsAt))
        {
            // A milestone is a moment, so it only collides with another at the same instant.
            var end = job.IsMilestone ? job.StartsAt.AddTicks(1) : job.EndsAt;
            var index = freeAt.FindIndex(free => free <= job.StartsAt);

            if (index < 0)
            {
                index = freeAt.Count;
                freeAt.Add(end);
            }
            else
            {
                freeAt[index] = end;
            }

            lane[job.Id] = index;
        }

        lanes = Math.Max(1, freeAt.Count);
        return lane;
    }

    /// <summary>
    /// The axis: the range the parent set, or the one worked out from the items, moved by however
    /// far Earlier and Later have paged it.
    /// </summary>
    private VetoGanttTimeline CreateTimeline ( IReadOnlyList<VetoGanttItem> visible )
    {
        VetoGanttTimeline Make ( DateOnly? start, DateOnly? end, DateOnly today ) =>
            VetoGanttTimeline.Create(visible, EffectiveScale, start, end, today, View,
                FitToWidth ? _viewportWidth : 0, StartTime, EndTime, MinHourWidth, HeaderFormat, HeaderGroupFormat);

        // Today alone, paged to another day: that day is drawn exactly as today would be.
        if (View == VetoGanttView.Today && Start is null && End is null)
        {
            return Make(null, null, Today.AddDays(_pagedDays));
        }

        var timeline = Make(Start, End, Today);

        return _pagedDays == 0
            ? timeline
            : Make(timeline.Start.AddDays(_pagedDays), timeline.End.AddDays(_pagedDays), Today);
    }

    /// <summary>The span the today view covers: today, or the explicit range, trimmed to the time window.</summary>
    private (DateTime From, DateTime To) ViewWindow ()
    {
        var first = (Start ?? Today).AddDays(_pagedDays);
        var last = (End ?? Today).AddDays(_pagedDays);
        var from = first.ToDateTime(StartTime ?? TimeOnly.MinValue);
        var to = EndTime is { } until
            ? last.ToDateTime(until)
            : last.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return (from, to);
    }

    /// <summary>
    /// Applies the text and category filters, keeping any ancestor of a matching row so a task
    /// never appears detached from the project it belongs to.
    /// </summary>
    private List<VetoGanttItem> Filter ()
    {
        var byId = AllItems.ToDictionary(i => i.Id);
        var (from, to) = ViewWindow();

        bool Matches ( VetoGanttItem item )
        {
            if (!string.IsNullOrWhiteSpace(Search)
                && !item.Name.Contains(Search, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_groupIds.Contains(item.Id))
            {
                return false;
            }

            if (ItemFilter is not null && !ItemFilter(item))
            {
                return false;
            }

            foreach (var filter in Filters)
            {
                if (_activeFilters.Contains(filter.Id) && !filter.Predicate(item))
                {
                    return false;
                }
            }

            if (View == VetoGanttView.Today)
            {
                // Today's work only: a row that finished yesterday or starts tomorrow is not on
                // today's list, however close it is - and with a time window, a job outside the
                // window is not either.
                var dates = Resolve(item);

                if (dates.StartsAt >= to || dates.EndsAt <= from)
                {
                    return false;
                }
            }

            return item.Category is null || !_hiddenCategories.Contains(item.Category);
        }

        var keep = new HashSet<string>();

        foreach (var item in AllItems.Where(Matches))
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

        return AllItems.Where(i => keep.Contains(i.Id)).ToList();
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
            VetoGanttSort.Start => items.OrderBy(i => i.StartsAt).ThenBy(i => i.Name),
            VetoGanttSort.End => items.OrderBy(i => i.EndsAt).ThenBy(i => i.Name),
            _ => items.OrderBy(i => i.Duration).ThenBy(i => i.Name)
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

        // Every descendant that is itself a piece of work rather than a group of them, in order.
        void Leaves ( string id, List<VetoGanttItem> into )
        {
            foreach (var child in children[id])
            {
                if (children.ContainsKey(child.Id))
                {
                    Leaves(child.Id, into);
                }
                else
                {
                    into.Add(child);
                }
            }
        }

        void Add ( VetoGanttItem item, int depth )
        {
            var hasChildren = children.ContainsKey(item.Id);
            var expanded = hasChildren && IsExpanded(item.Id);

            List<VetoGanttItem> inline = [];

            if (hasChildren && !expanded && SubItemLayout == VetoGanttSubItemLayout.Inline)
            {
                Leaves(item.Id, inline);
            }

            var lanes = 1;
            var lane = StackOverlaps && inline.Count > 1
                ? AssignLanes(inline, out lanes)
                : new Dictionary<string, int>();

            rows.Add(new Row(item, depth, hasChildren, inline, lanes, lane));

            if (!expanded)
            {
                return;
            }

            foreach (var child in children[item.Id])
            {
                Add(child, depth + 1);
            }
        }

        var ids = items.Select(i => i.Id).ToHashSet();
        var roots = items.Where(i => i.ParentId is null || !ids.Contains(i.ParentId));

        foreach (var root in Order(roots))
        {
            Add(root, 0);
        }

        return rows;
    }

    private bool IsExpanded ( string id ) => SubItemLayout == VetoGanttSubItemLayout.Inline
        ? Collapsible && _expanded.Contains(id)
        : !Collapsible || !_collapsed.Contains(id);

    private void SetExpanded ( string id, bool expanded )
    {
        if (expanded)
        {
            _collapsed.Remove(id);
            _expanded.Add(id);
        }
        else
        {
            _expanded.Remove(id);
            _collapsed.Add(id);
        }
    }

    private bool IsSelected ( VetoGanttItem item ) => SelectedId == item.Id;

    private bool IsCategoryOn ( string category ) => !_hiddenCategories.Contains(category);

    /// <summary>The inline style that gives a filter chip its category's colour.</summary>
    private static string CategoryStyle ( Category category )
        => $"--veto-gantt-chip-color: {category.Color}";

    private void ToggleGroup ( string id )
    {
        SetExpanded(id, !IsExpanded(id));
        Rebuild();
    }

    private Task ToggleCategoryAsync ( string category ) => SetCategoryVisibleAsync(category, !IsCategoryOn(category));

    private async Task OnSearchChangedAsync ( string? value )
    {
        Search = value;
        Rebuild();
        await SearchChanged.InvokeAsync(value);
        await RaiseFilterChangedAsync();
    }

    private bool IsFilterOn ( string id ) => _activeFilters.Contains(id);

    /// <summary>Tells the page about a filter change: the bound value that moved, then the whole picture.</summary>
    private Task RaiseFilterChangedAsync ()
        => OnFilterChanged.InvokeAsync(new VetoGanttFilterState(
            Search,
            _hiddenCategories.ToArray(),
            _activeFilters.ToArray(),
            Filter()));

    /// <summary>Sets the text filter, as typing in the toolbar does.</summary>
    public async Task SetSearchAsync ( string? text )
    {
        await OnSearchChangedAsync(text);
        StateHasChanged();
    }

    /// <summary>Switches a category on or off, as its toolbar chip does.</summary>
    public async Task SetCategoryVisibleAsync ( string category, bool visible )
    {
        var changed = visible ? _hiddenCategories.Remove(category) : _hiddenCategories.Add(category);

        if (!changed)
        {
            return;
        }

        _hiddenCategoriesParameter = _hiddenCategories.ToArray();
        Rebuild();
        StateHasChanged();

        await HiddenCategoriesChanged.InvokeAsync(_hiddenCategoriesParameter);
        await RaiseFilterChangedAsync();
    }

    /// <summary>Switches one of the <see cref="Filters"/> on or off, as its toolbar toggle does.</summary>
    public async Task SetFilterActiveAsync ( string id, bool active )
    {
        var changed = active ? _activeFilters.Add(id) : _activeFilters.Remove(id);

        if (!changed)
        {
            return;
        }

        _activeFiltersParameter = _activeFilters.ToArray();
        Rebuild();
        StateHasChanged();

        await ActiveFiltersChanged.InvokeAsync(_activeFiltersParameter);
        await RaiseFilterChangedAsync();
    }

    private Task ToggleFilterAsync ( string id ) => SetFilterActiveAsync(id, !IsFilterOn(id));

    /// <summary>
    /// Clears every filter the reader controls - the text, the categories and the app's own
    /// toggles - raising each changed binding once and <see cref="OnFilterChanged"/> once.
    /// <see cref="ItemFilter"/> stays: the reader never set it.
    /// </summary>
    public async Task ClearFiltersAsync ()
    {
        var hadSearch = !string.IsNullOrEmpty(Search);
        var hadCategories = _hiddenCategories.Count > 0;
        var hadFilters = _activeFilters.Count > 0;

        if (!hadSearch && !hadCategories && !hadFilters)
        {
            return;
        }

        Search = null;
        _hiddenCategories.Clear();
        _activeFilters.Clear();
        _hiddenCategoriesParameter = [];
        _activeFiltersParameter = [];

        Rebuild();
        StateHasChanged();

        if (hadSearch)
        {
            await SearchChanged.InvokeAsync(null);
        }

        if (hadCategories)
        {
            await HiddenCategoriesChanged.InvokeAsync([]);
        }

        if (hadFilters)
        {
            await ActiveFiltersChanged.InvokeAsync([]);
        }

        await RaiseFilterChangedAsync();
    }

    /// <summary>Changes the zoom level and scrolls back to today, as the scale picker does.</summary>
    public async Task SetScaleAsync ( VetoGanttScale scale )
    {
        if (Scale == scale)
        {
            return;
        }

        Scale = scale;
        Rebuild();
        StateHasChanged();

        // The handler was given the old pixels-per-day and snap, so it is reattached with the
        // new ones on the next render.
        _dragAttached = false;

        await ScaleChanged.InvokeAsync(scale);

        // The axis has been redrawn at a different width, so the previous scroll position no
        // longer points anywhere meaningful.
        _scrollToNowPending = true;
    }

    private async Task SelectAsync ( VetoGanttItem item )
    {
        SelectedId = item.Id;

        await SelectedIdChanged.InvokeAsync(item.Id);
        await OnItemSelected.InvokeAsync(item);
    }

    // ------------------------------------------------------------------ Imperative API
    //
    // For a caller holding an @ref: the same actions the toolbar and the bars offer, so a page
    // can drive the chart from its own buttons, shortcuts or state. Each one re-renders, since a
    // call from outside the chart's own event handlers would not otherwise be drawn.

    /// <summary>
    /// Selects a row by id, raising <see cref="SelectedIdChanged"/> and
    /// <see cref="OnItemSelected"/> as a click on its bar would. Null clears the selection.
    /// An id that matches no item is ignored.
    /// </summary>
    public async Task SelectAsync ( string? id )
    {
        if (id is null)
        {
            SelectedId = null;
            StateHasChanged();
            await SelectedIdChanged.InvokeAsync(null);
            return;
        }

        if (AllItems.FirstOrDefault(i => i.Id == id) is { } item)
        {
            await SelectAsync(item);
            StateHasChanged();
        }
    }

    /// <summary>Opens a collapsed row so its children show.</summary>
    public void Expand ( string id ) => SetCollapsed([id], collapsed: false);

    /// <summary>Folds a row's children away under it.</summary>
    public void Collapse ( string id ) => SetCollapsed([id], collapsed: true);

    /// <summary>Opens every row that has children.</summary>
    public void ExpandAll () => SetCollapsed(ParentIds(), collapsed: false);

    /// <summary>Folds every row that has children, leaving only the top level.</summary>
    public void CollapseAll () => SetCollapsed(ParentIds(), collapsed: true);

    private IEnumerable<string> ParentIds ()
    {
        var parents = AllItems.Where(i => i.ParentId is not null).Select(i => i.ParentId!).ToHashSet();
        return AllItems.Where(i => parents.Contains(i.Id)).Select(i => i.Id);
    }

    private void SetCollapsed ( IEnumerable<string> ids, bool collapsed )
    {
        foreach (var id in ids)
        {
            SetExpanded(id, !collapsed);
        }

        Rebuild();
        StateHasChanged();
    }

    /// <summary>
    /// Opens the <see cref="TooltipTemplate"/> card on a row, as hovering its bar would - to
    /// point something out, or to walk a reader through the chart. Does nothing without a
    /// template: the browser's native tooltip cannot be opened from code.
    /// </summary>
    public void ShowTooltip ( string id )
    {
        if (!UsesTooltipTemplate)
        {
            return;
        }

        _hoveredId = id;
        StateHasChanged();
    }

    private string CellStyle ( VetoGanttTimeline.Column column )
        => string.Create(CultureInfo.InvariantCulture, $"left: {column.Offset:0.##}px; width: {column.Width:0.##}px");

    private string BarStyle ( VetoGanttItem item ) => BarStyle(item, lane: 0, lanes: 1);

    private string BarStyle ( VetoGanttItem item, int lane, int lanes )
    {
        // In a row of stacked lanes a bar is centred on its own lane rather than the row; the
        // stylesheet works that out from the row's height, whatever the row's height is.
        var laneTop = lanes > 1
            ? string.Create(CultureInfo.InvariantCulture, $"; --veto-gantt-lane: {lane}")
            : string.Empty;

        return BarStyleCore(item) + laneTop;
    }

    private string BarStyleCore ( VetoGanttItem item )
    {
        var (offset, width) = Timeline.Place(item.StartsAt, item.EndsAt);
        var color = item.Color ?? BarColor;

        if (item.IsMilestone)
        {
            offset = Timeline.OffsetAt(item.StartsAt);

            return string.Create(CultureInfo.InvariantCulture,
                $"left: {offset:0.##}px; --veto-gantt-item-color: {color}");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"left: {offset:0.##}px; width: {width:0.##}px; --veto-gantt-item-color: {color}");
    }

    private string FormatRange ( VetoGanttItem item )
    {
        if (!item.HasTimes)
        {
            return item.IsMilestone || item.Start == item.End
                ? item.Start.ToString("d MMM")
                : $"{item.Start:d MMM} – {item.End:d MMM}";
        }

        var from = item.StartsAt;
        var to = item.EndsAt;
        var until = to.TimeOfDay == TimeSpan.Zero ? "24:00" : to.ToString("HH:mm");

        // On the today view every row is today's, so the day would only crowd the times out.
        var withinToday = from.Date == ShownDay.ToDateTime(TimeOnly.MinValue)
            && (to.Date == from.Date || (to.TimeOfDay == TimeSpan.Zero && to.Date == from.Date.AddDays(1)));

        if (View == VetoGanttView.Today && withinToday)
        {
            return item.IsMilestone ? $"{from:HH:mm}" : $"{from:HH:mm}–{until}";
        }

        if (item.IsMilestone)
        {
            return $"{from:d MMM HH:mm}";
        }

        // A job inside one day names the day once; one that runs overnight names both.
        return from.Date == to.Date || (to.TimeOfDay == TimeSpan.Zero && to.Date == from.Date.AddDays(1))
            ? $"{from:d MMM} {from:HH:mm}–{until}"
            : $"{from:d MMM HH:mm} – {to:d MMM HH:mm}";
    }

    /// <summary>What goes in a bar's native <c>title</c>: nothing when a template or no tooltip is wanted.</summary>
    private string? NativeTooltipFor ( VetoGanttItem item )
    {
        if (!ShowTooltips || TooltipTemplate is not null)
        {
            return null;
        }

        return TooltipText is null ? TooltipFor(item) : TooltipText(item);
    }

    // The row whose bar the pointer or keyboard is on, for the templated tooltip.
    private string? _hoveredId;

    private bool UsesTooltipTemplate => ShowTooltips && TooltipTemplate is not null;

    /// <summary>
    /// The hover and focus handlers, only when a templated tooltip needs them. Every UI event
    /// re-renders the chart, so a chart without a template should not pay for pointer movement.
    /// </summary>
    private IReadOnlyDictionary<string, object>? TooltipHandlers ( VetoGanttItem item )
    {
        if (!UsesTooltipTemplate)
        {
            return null;
        }

        return new Dictionary<string, object>
        {
            ["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => _hoveredId = item.Id),
            ["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, () => _hoveredId = item.Id),
            ["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, HideTooltip),
            ["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(this, HideTooltip),
            // A drag moves the bar out from under the card, so the card goes the moment one starts.
            ["onpointerdown"] = EventCallback.Factory.Create<PointerEventArgs>(this, HideTooltip),
            ["aria-describedby"] = _hoveredId == item.Id ? $"{ElementId}-tooltip" : null!
        };
    }

    /// <summary>Closes the <see cref="TooltipTemplate"/> card, however it was opened.</summary>
    public void HideTooltip ()
    {
        _hoveredId = null;
        StateHasChanged();
    }

    /// <summary>The item the templated tooltip belongs to, and the row it is drawn on.</summary>
    private (VetoGanttItem Item, int Index)? Hovered
    {
        get
        {
            if (!UsesTooltipTemplate || _hoveredId is null)
            {
                return null;
            }

            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].Item.Id == _hoveredId)
                {
                    return (Rows[i].Item, i);
                }

                if (Rows[i].Inline.FirstOrDefault(item => item.Id == _hoveredId) is { } inline)
                {
                    return (inline, i);
                }
            }

            return null;
        }
    }

    private ElementReference _tooltipCard;

    /// <summary>The card's own layer, when the app set one.</summary>
    private string? TooltipLayerStyle
        => TooltipZIndex is { } z ? $"--veto-gantt-tooltip-z: {z.ToString(CultureInfo.InvariantCulture)}" : null;

    /// <summary>
    /// Places the open card against its bar, in window coordinates: below it, or above when
    /// there is no room below, and kept inside the window. Done by the page because only the
    /// page knows where the bar is on screen once the chart and the page have scrolled.
    /// </summary>
    private Task PlaceTooltipAsync ()
        => Hovered is { } hovered
            ? InvokeModuleAsync("placeTooltip", _tooltipCard, RootElement, hovered.Item.Id)
            : Task.CompletedTask;

    private string TooltipFor ( VetoGanttItem item )
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
            _scrollToNowPending = ScrollToTodayOnLoad;
            StartClock();
        }

        await ObserveWidthAsync();

        // Scrolled only once the axis is drawn at its real width. Until the chart has heard how
        // wide it is, the axis is at the scale's own density, and a position worked out against
        // that would miss the marker once the axis is fitted.
        if (_scrollToNowPending && ScrollToTodayOnLoad && (_viewportWidth > 0 || !FitToWidth))
        {
            _scrollToNowPending = false;
            await InvokeModuleAsync("scrollTo", _scroller, TodayMarkerOffset ?? 0, false, 0.5);
        }

        // Back from a paged range, the now marker is found on the range as redrawn.
        if (_centreNowPending)
        {
            _centreNowPending = false;
            await InvokeModuleAsync("scrollTo", _scroller, TodayMarkerOffset ?? 0, true, 0.5);
        }

        // A paged range opens at the edge PageAsync chose for it.
        if (_pagedScrollPending != 0)
        {
            await InvokeModuleAsync("scrollToEdge", _scroller, _pagedScrollPending > 0);
            _pagedScrollPending = 0;
        }

        if (_contextMenuOpening)
        {
            _contextMenuOpening = false;
            await InvokeModuleAsync("openMenu", _contextMenuPanel);
        }

        await PlaceTooltipAsync();
        await SyncDragHandlesAsync();
    }

    // ------------------------------------------------------------------ The item menu

    private void OpenContextMenu ( MouseEventArgs args, VetoGanttItem item )
    {
        if (ItemContextMenu is null)
        {
            return;
        }

        _contextMenu = (item, args.ClientX, args.ClientY);
        _contextMenuOpening = true;
    }

    private void CloseContextMenu () => _contextMenu = null;

    private async Task OnContextMenuKeyDownAsync ( KeyboardEventArgs args )
    {
        switch (args.Key)
        {
            case "Escape" or "Tab":
                CloseContextMenu();
                break;
            case "ArrowDown":
                await InvokeModuleAsync("moveMenuFocus", _contextMenuPanel, 1);
                break;
            case "ArrowUp":
                await InvokeModuleAsync("moveMenuFocus", _contextMenuPanel, -1);
                break;
        }
    }

    // ------------------------------------------------------------------ Fitting the width

    private double _viewportWidth;

    // A scroll to the now marker waiting for the next render at the chart's real width.
    private bool _scrollToNowPending;
    private string? _observedScrollerId;

    /// <summary>
    /// Starts watching the timeline's width, so a short axis can be stretched to fill it. The
    /// scroller is a new element after a loading placeholder, so it is re-observed whenever it
    /// changes.
    /// </summary>
    private async Task ObserveWidthAsync ()
    {
        if (!FitToWidth || _scroller.Id is null || _scroller.Id == _observedScrollerId)
        {
            return;
        }

        _observedScrollerId = _scroller.Id;
        _selfRef ??= DotNetObjectReference.Create(this);

        await InvokeModuleAsync("observeWidth", _scroller, _selfRef);
    }

    /// <summary>Called from the page when the timeline's visible width changes.</summary>
    [JSInvokable]
    public Task OnViewportResized ( double width )
    {
        // Sub-pixel jitter while a layout settles would otherwise re-render the whole chart.
        if (Math.Abs(width - _viewportWidth) < 1)
        {
            return Task.CompletedTask;
        }

        // The first width the chart hears changes the axis under a scroll that was waiting for it.
        _scrollToNowPending |= _viewportWidth == 0;
        _viewportWidth = width;

        return InvokeAsync(() =>
        {
            Rebuild();
            StateHasChanged();
        });
    }

    /// <summary>Switches between today's work and the whole schedule, as the toolbar switch does.</summary>
    public async Task SetViewAsync ( VetoGanttView view )
    {
        if (View == view)
        {
            return;
        }

        View = view;
        _rangeParameters = (Start, End, View);
        _pagedDays = 0;
        Rebuild();
        StateHasChanged();

        await ViewChanged.InvokeAsync(view);

        _scrollToNowPending = true;
    }

    /// <summary>
    /// Scrolls the now marker to the centre of the timeline, or to the start of the axis when
    /// today is outside it. A range that Earlier or Later has paged away is moved back first.
    /// </summary>
    public async Task ScrollToTodayAsync ()
    {
        // A parent that takes each paged range back as its own Start and End leaves nothing
        // paged to undo, so a range without today on it is paged by whole steps until it has.
        var back = _pagedDays != 0 ? -_pagedDays : PageBeyondRange ? DaysToToday() : 0;

        if (back != 0)
        {
            _pagedDays += back;
            Rebuild();
            _centreNowPending = true;
            StateHasChanged();

            await OnRangeChanged.InvokeAsync(new VetoGanttRange(Timeline.Start, Timeline.End));
            return;
        }

        var offset = TodayMarkerOffset ?? 0;
        await InvokeModuleAsync("scrollTo", _scroller, offset, true, 0.5);
    }

    /// <summary>How far to page the axis, in the steps Earlier and Later take, to bring today onto it.</summary>
    private int DaysToToday ()
    {
        var length = Timeline.End.DayNumber - Timeline.Start.DayNumber + 1;
        var step = Math.Min(length, EffectiveScale.StepDays());

        return Today > Timeline.End ? (Today.DayNumber - Timeline.End.DayNumber + step - 1) / step * step
            : Today < Timeline.Start ? -((Timeline.Start.DayNumber - Today.DayNumber + step - 1) / step * step)
            : 0;
    }

    /// <summary>
    /// Scrolls by a screenful in either direction, or - with nothing left to scroll that way -
    /// pages the range, when <see cref="PageBeyondRange"/> allows it.
    /// </summary>
    private async Task ScrollByAsync ( int direction )
    {
        var scrolled = await InvokeModuleAsync<bool?>("scrollBy", _scroller, direction * 0.8);

        // Null is no answer from the page at all - prerendering - where paging would be a guess.
        if (scrolled is not false || !PageBeyondRange)
        {
            return;
        }

        await PageAsync(direction);
    }

    /// <summary>
    /// Moves the range a page earlier or later. A page is the scale's step, but never longer
    /// than the range itself: a range of five days fitted to the width moves five days, so the
    /// next page starts the day after this one ends. The today view moves a day at a time.
    /// </summary>
    private async Task PageAsync ( int direction )
    {
        var length = Timeline.End.DayNumber - Timeline.Start.DayNumber + 1;
        var step = Math.Min(length, EffectiveScale.StepDays());

        _pagedDays += direction * step;

        // A whole new page is read from its beginning going on, and from its end going back. A
        // range that only slid is read on from the edge it slid past, where the new days are.
        _pagedScrollPending = step >= length ? -direction : direction;
        Rebuild();
        StateHasChanged();

        await OnRangeChanged.InvokeAsync(new VetoGanttRange(Timeline.Start, Timeline.End));
    }

    /// <summary>Brings a date into view. A date outside the axis scrolls to the nearer end.</summary>
    public Task ScrollToDateAsync ( DateOnly date )
    {
        var offset = Timeline.Contains(date) ? Timeline.OffsetOf(date)
            : date < Timeline.Start ? 0
            : Timeline.Width;

        return InvokeModuleAsync("scrollTo", _scroller, offset);
    }

    /// <summary>Scrolls back by most of a screen, or pages the range back, as the Earlier button does.</summary>
    public Task ScrollEarlierAsync () => ScrollByAsync(-1);

    /// <summary>Scrolls forward by most of a screen, or pages the range on, as the Later button does.</summary>
    public Task ScrollLaterAsync () => ScrollByAsync(1);

    // ------------------------------------------------------------------ Export

    /// <summary>
    /// Exports the rows as currently filtered. The app gets first refusal through
    /// <see cref="OnExport"/>; if it does not take it, the file is downloaded here.
    /// </summary>
    public async Task ExportAsync ( VetoGanttExportFormat format )
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
        csv.AppendLine("Id,Name,Parent,Start,End,Days,Category,Progress,Milestone,StartTime,EndTime");

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
               .Append(item.IsMilestone ? "true" : "false").Append(',')
               .Append(item.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
               .Append(item.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty)
               .AppendLine();
        }

        return csv.ToString();
    }

    /// <summary>Renders the rows as JSON.</summary>
    internal static string ToJson ( IReadOnlyList<VetoGanttItem> items ) =>
        JsonSerializer.Serialize(
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
                Milestone = i.IsMilestone,
                StartTime = i.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                EndTime = i.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture)
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

    private async Task<T?> InvokeModuleAsync<T> ( string function, params object?[] args )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Gantt/veto-gantt.js");
            return await _module.InvokeAsync<T>(function, args);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // As below: no page to answer, so no answer.
            return default;
        }
    }

    private async Task InvokeModuleAsync ( string function, params object?[] args )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Gantt/veto-gantt.js");
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
        _clock?.Dispose();

        if (_observedScrollerId is not null)
        {
            await InvokeModuleAsync("unobserveWidth", _scroller);
        }

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

    /// <summary>
    /// One line of the chart. <c>Inline</c> holds the sub-items drawn on this row's line instead
    /// of on rows of their own - empty unless the row is a folded parent in the inline layout.
    /// </summary>
    private sealed record Row ( VetoGanttItem Item, int Depth, bool HasChildren, IReadOnlyList<VetoGanttItem> Inline, int Lanes, IReadOnlyDictionary<string, int> Lane )
    {
        /// <summary>The lane an inline job is drawn on; zero for everything else.</summary>
        public int LaneOf ( VetoGanttItem item ) => Lane.TryGetValue(item.Id, out var lane) ? lane : 0;
    }

    /// <summary>A category and the colour the rows in it are drawn with.</summary>
    private sealed record Category ( string Name, string Color );

    // ------------------------------------------------------------------ Editing

    /// <summary>
    /// The dates a row is drawn at: this reader's saved edit if there is one, otherwise what the
    /// app supplied. Applied at render time rather than by rewriting the list, which the chart
    /// does not own.
    /// </summary>
    private VetoGanttItem Resolve ( VetoGanttItem item ) =>
        _overrides.TryGetValue(item.Id, out var dates)
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
        var item = AllItems.FirstOrDefault(i => i.Id == id);

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
        if (!Editable || args.Key is not ("ArrowLeft" or "ArrowRight") || !IsEditable(item, IsSummary(item)))
        {
            return;
        }

        var step = EffectiveScale.SnapDays() * (args.Key == "ArrowRight" ? 1 : -1);
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
            _storeModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Gantt/veto-gantt-store.js");
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
    private bool IsEditable ( VetoGanttItem item, bool isSummary ) => Editable && !isSummary && !item.IsMilestone;

    /// <summary>Whether a row is drawn as a summary: one with children of its own, or a group.</summary>
    private bool IsSummary ( VetoGanttItem item )
        => _groupIds.Contains(item.Id) || AllItems.Any(i => i.ParentId == item.Id);

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

        // Every hover and click re-renders, and nothing the handler measures changed in most of
        // them - so it is only re-attached when the grid, the density, the snap or the resizing did.
        var attachment = (_grid.Id, Timeline.PixelsPerDay, EffectiveScale.SnapDays(), Resizable);

        if (_dragAttached && attachment == _dragAttachment)
        {
            return;
        }

        try
        {
            _dragModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Gantt/veto-gantt-drag.js");
            _selfRef ??= DotNetObjectReference.Create(this);

            await _dragModule.InvokeVoidAsync("attach", _grid, _selfRef, new
            {
                pixelsPerDay = Timeline.PixelsPerDay,
                snapDays = EffectiveScale.SnapDays(),
                resizable = Resizable
            });

            _dragAttachment = attachment;

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
    private sealed record Link ( string FromId, string ToId, double FromX, double FromY, double ToX, double ToY );
}