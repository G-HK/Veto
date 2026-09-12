using System.Globalization;
using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Cron;

/// <summary>
/// A visual builder for a cron expression, with the expression read back in plain language and
/// the next few runs worked out.
/// </summary>
/// <remarks>
/// <para>
/// The tabs are the familiar phrasings of a schedule - every <i>n</i> minutes, on these
/// weekdays at this time - and each writes the expression underneath. The expression stays
/// visible and editable throughout, so the builder teaches cron rather than hiding it, and
/// anything the tabs cannot say can still be typed.
/// </para>
/// <para>
/// <b>The description.</b> Built in, and English only, so the component says something useful
/// the moment it is dropped into an app. For the two dozen languages
/// <a href="https://github.com/bradymholt/cron-expression-descriptor">CronExpressionDescriptor</a>
/// covers, hand its <c>ExpressionDescriptor.GetDescription</c> to <see cref="Describer"/>. The
/// parameter exists so that package can replace the built-in describer <em>without the library
/// taking a dependency on it</em> - Veto ships with none, and this is not the feature to break
/// that for.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoCron @bind-Value="_schedule" /&gt;
///
/// @* With CronExpressionDescriptor, in the consumer's own project: *@
/// &lt;VetoCron @bind-Value="_schedule"
///           Describer="@(cron =&gt; ExpressionDescriptor.GetDescription(cron))" /&gt;
/// </code>
/// </example>
public partial class VetoCron : VetoComponentBase
{
    private static readonly string[] DayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    private static readonly string[] MonthLabels =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    private static readonly VetoCronMode[] AllModes = Enum.GetValues<VetoCronMode>();

    // One interval per shape, not one shared between them: "every 5" means minutes on one tab
    // and days on another, and sharing the field turned "every 5 minutes" into "every 5 days"
    // the moment the user switched tab.
    private int _everyMinutes = 5;
    private int _everyHours = 1;
    private int _everyDays = 1;
    private int _everyMonths = 1;
    private int _minute;
    private int _hour = 9;
    private int _dayOfMonth = 1;
    private int _month = 1;
    private readonly SortedSet<int> _weekdays = [1, 2, 3, 4, 5];

    private VetoCronExpression? _parsed;
    private string? _error;
    private string? _lastParsed;

    /// <inheritdoc />
    protected override string RootClass => "veto-cron";

    /// <summary>The cron expression. Use <c>@bind-Value</c>.</summary>
    [Parameter]
    public string Value { get; set; } = "0 9 * * *";

    /// <summary>Raised when the expression changes, so <c>@bind-Value</c> works.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Raised when the expression changes and is valid, with the parsed form.</summary>
    [Parameter]
    public EventCallback<VetoCronExpression> OnValidChanged { get; set; }

    /// <summary>
    /// Turns an expression into a sentence. Null uses <see cref="VetoCronDescriber"/>, which is
    /// English only; return null from your own describer to fall back to it.
    /// </summary>
    [Parameter]
    public Func<string, string?>? Describer { get; set; }

    /// <summary>Which tabs are offered, in order. Null offers all of them.</summary>
    [Parameter]
    public IReadOnlyList<VetoCronMode>? AllowedModes { get; set; }

    /// <summary>The tab shown first. Ignored once the user picks one.</summary>
    [Parameter]
    public VetoCronMode InitialMode { get; set; } = VetoCronMode.Daily;

    /// <summary>Whether the tabs are shown at all. False leaves the raw expression and its description.</summary>
    [Parameter]
    public bool ShowModes { get; set; } = true;

    /// <summary>Whether the raw expression is shown and editable.</summary>
    [Parameter]
    public bool ShowExpression { get; set; } = true;

    /// <summary>Whether the expression is read back in words.</summary>
    [Parameter]
    public bool ShowDescription { get; set; } = true;

    /// <summary>Whether the next few runs are listed.</summary>
    [Parameter]
    public bool ShowNextRuns { get; set; } = true;

    /// <summary>How many upcoming runs to list.</summary>
    [Parameter]
    public int NextRunCount { get; set; } = 5;

    /// <summary>How each upcoming run is formatted.</summary>
    [Parameter]
    public string NextRunFormat { get; set; } = "ddd dd MMM yyyy, HH:mm";

    /// <summary>The heading above the list of upcoming runs.</summary>
    [Parameter]
    public string NextRunsLabel { get; set; } = "Next runs";

    /// <summary>
    /// The moment the upcoming runs are counted from. Null uses <see cref="DateTime.Now"/> -
    /// set it to pin the list, which is what makes a screenshot or a test reproducible.
    /// </summary>
    [Parameter]
    public DateTime? Now { get; set; }

    /// <summary>The culture the runs are formatted in. Null uses the current culture.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Whether the expression carries a leading seconds field. Only affects what the raw
    /// expression is parsed as; the tabs always build five-field expressions.
    /// </summary>
    [Parameter]
    public bool IncludeSeconds { get; set; }

    /// <summary>Text above the builder.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Guidance under the builder.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>When true nothing can be changed and the controls cannot be focused.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>When true the builder can be read and focused but not changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The builder's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>
    /// When false every border is dropped - the box around the builder and the box around each
    /// control inside it - leaving the schedule reading as a sentence on the page rather than
    /// as a form. The focus ring stays: the borders are decoration, but the ring is how a
    /// keyboard user knows where they are.
    /// </summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>The tab currently shown.</summary>
    public VetoCronMode Mode { get; private set; }

    /// <summary>True when the current expression parses.</summary>
    public bool IsValid => _parsed is not null;

    /// <summary>The parsed expression, or null when it does not parse.</summary>
    public VetoCronExpression? Expression => _parsed;

    private IReadOnlyList<VetoCronMode> Modes => AllowedModes is { Count: > 0 } ? AllowedModes : AllModes;

    private string ExpressionId => $"{ElementId}-expression";

    private string MessageId => $"{ElementId}-message";

    private string FieldLegend => IncludeSeconds ? "sec  min  hour  day  month  weekday" : "min  hour  day  month  weekday";

    private string Description
        => _parsed is null
            ? _error ?? "Not a valid cron expression."
            : Describer?.Invoke(_parsed.Text) ?? VetoCronDescriber.Describe(_parsed);

    /// <summary>The thing about the expression worth warning about, or null when there is nothing.</summary>
    private string? Warning
        => _parsed is { HasAmbiguousDayRule: true }
            ? "Both day fields are set, so this fires when either matches - not only when both do."
            : null;

    private IReadOnlyList<DateTime> NextRuns
        => _parsed is null ? [] : VetoCronSchedule.Next(_parsed, Now ?? DateTime.Now, NextRunCount);

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        Mode = InitialMode;
        Adopt(Value);
    }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        // Re-parse only when the expression actually changed, so a re-render for any other
        // reason does not throw away an error the user is still looking at.
        if (_lastParsed != Value)
        {
            Parse(Value);
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("disabled"), Disabled)
            .AddClass(Modifier("readonly"), ReadOnly)
            .AddClass(Modifier("invalid"), !IsValid)
            .AddClass(Modifier("borderless"), !Bordered);

    private void Parse ( string? text )
    {
        _lastParsed = text;
        _parsed = VetoCronExpression.TryParse(text, out var expression, out var error,
            IncludeSeconds ? true : null)
            ? expression
            : null;
        _error = error;
    }

    /// <summary>
    /// Reads the builder's controls back out of an expression, so switching to a tab shows what
    /// the current schedule actually says rather than resetting it to a default.
    /// </summary>
    private void Adopt ( string? text )
    {
        Parse(text);

        if (_parsed is null)
        {
            return;
        }

        if (_parsed.Minutes.IsSingle)
        {
            _minute = _parsed.Minutes.Values.Single();
        }

        if (_parsed.Hours.IsSingle)
        {
            _hour = _parsed.Hours.Values.Single();
        }

        if (_parsed.DaysOfMonth.IsSingle)
        {
            _dayOfMonth = _parsed.DaysOfMonth.Values.Single();
        }

        if (_parsed.Months.IsSingle)
        {
            _month = _parsed.Months.Values.Single();
        }

        if (StepOf(_parsed.Minutes) is { } minuteStep)
        {
            _everyMinutes = minuteStep;
        }

        if (StepOf(_parsed.Hours) is { } hourStep)
        {
            _everyHours = hourStep;
        }

        if (StepOf(_parsed.DaysOfMonth) is { } dayStep)
        {
            _everyDays = dayStep;
        }

        if (StepOf(_parsed.Months) is { } monthStep)
        {
            _everyMonths = monthStep;
        }

        if (!_parsed.DaysOfWeek.IsUnrestricted)
        {
            _weekdays.Clear();

            foreach (var day in _parsed.DaysOfWeek.Values)
            {
                _weekdays.Add(day);
            }
        }
    }

    /// <summary>The step a field was written with, when it is a plain <c>*/n</c>.</summary>
    private static int? StepOf ( VetoCronField field )
        => field.Parts.Count == 1 && field.Parts[0] is { Kind: VetoCronPartKind.Step } part && part.Step > 1
            ? part.Step
            : null;

    private async Task SelectModeAsync ( VetoCronMode mode )
    {
        if (Disabled || ReadOnly || Mode == mode)
        {
            return;
        }

        Adopt(Value);
        Mode = mode;

        if (mode != VetoCronMode.Advanced)
        {
            await RebuildAsync();
        }
    }

    private async Task ToggleDayAsync ( int day )
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        if (!_weekdays.Remove(day))
        {
            _weekdays.Add(day);
        }

        // An empty weekly schedule would silently become "every day", which is not what the
        // user asked for by unticking the last box.
        if (_weekdays.Count == 0)
        {
            _weekdays.Add(day);
            return;
        }

        await RebuildAsync();
    }

    private Task SetNumberAsync ( ChangeEventArgs args, int min, int max, Func<int, Task> set )
    {
        if (Disabled || ReadOnly ||
            !int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Task.CompletedTask;
        }

        return set(Math.Clamp(value, min, max));
    }

    /// <summary>Writes the current tab's controls back out as an expression.</summary>
    private Task RebuildAsync () => SetValueAsync(Build());

    private string Build () => Mode switch
    {
        VetoCronMode.Minutes => $"*/{_everyMinutes} * * * *",
        VetoCronMode.Hourly => _everyHours == 1
            ? $"{_minute} * * * *"
            : $"{_minute} */{_everyHours} * * *",
        VetoCronMode.Daily => _everyDays == 1
            ? $"{_minute} {_hour} * * *"
            : $"{_minute} {_hour} */{_everyDays} * *",
        VetoCronMode.Weekly => $"{_minute} {_hour} * * {string.Join(',', _weekdays)}",
        VetoCronMode.Monthly => _everyMonths == 1
            ? $"{_minute} {_hour} {_dayOfMonth} * *"
            : $"{_minute} {_hour} {_dayOfMonth} */{_everyMonths} *",
        VetoCronMode.Yearly => $"{_minute} {_hour} {_dayOfMonth} {_month} *",
        _ => Value
    };

    private Task OnExpressionInputAsync ( ChangeEventArgs args )
    {
        if (Disabled || ReadOnly)
        {
            return Task.CompletedTask;
        }

        // Typing by hand is the Advanced tab by definition - leaving the tab where it was would
        // let the next control change silently overwrite what was typed.
        Mode = VetoCronMode.Advanced;

        return SetValueAsync(args.Value?.ToString() ?? string.Empty);
    }

    private async Task SetValueAsync ( string next )
    {
        if (next == Value)
        {
            return;
        }

        Value = next;
        Parse(next);

        await ValueChanged.InvokeAsync(next);

        if (_parsed is not null && OnValidChanged.HasDelegate)
        {
            await OnValidChanged.InvokeAsync(_parsed);
        }
    }
}
