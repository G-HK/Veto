using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Slider;

/// <summary>
/// A value picked by dragging along a rail. Bound to one number by default; set
/// <see cref="Range"/> and it grows a second thumb and picks a span between two numbers.
/// </summary>
/// <remarks>
/// <para>
/// Built on native <c>&lt;input type="range"&gt;</c> elements - two of them, stacked, in range
/// mode - so keyboard operation, pointer capture and touch come from the browser rather than
/// from a pile of interop. Only the rail, the fill and the marks are drawn by Veto.
/// </para>
/// <para>
/// Marks are the notches on the rail. Supply them yourself for a named scale
/// (<c>"Low"</c>, <c>"Medium"</c>, <c>"High"</c>), or set <see cref="ShowMarks"/> and let the
/// component lay one out every <see cref="MarkEvery"/> - or every <see cref="Step"/> - and
/// label it with its own number.
/// </para>
/// </remarks>
/// <typeparam name="TValue">Any number type: <c>int</c>, <c>double</c>, <c>decimal</c>, and so on.</typeparam>
/// <example>
/// <code>
/// &lt;VetoSlider TValue="int" Label="Volume" @bind-Value="_volume" /&gt;
/// &lt;VetoSlider TValue="int" Range="true" Label="Price" @bind-Value="_low" @bind-UpperValue="_high" /&gt;
/// </code>
/// </example>
public partial class VetoSlider<TValue> : VetoInputBase<TValue>
    where TValue : struct, INumber<TValue>
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // The browser owns the DOM value of a range input while the user drags it. When we refuse
    // or adjust what it reports - read-only, a clamp against the other thumb, a snap to a mark -
    // our model value can come out identical to the previous render, so Blazor's diff finds
    // nothing to patch and the thumb is left sitting where the pointer dropped it. Alternating
    // the *format* of the rendered value makes the attribute string differ even when the number
    // does not, which forces the patch and snaps the thumb back to the truth.
    private bool _resync;

    private IReadOnlyList<VetoSliderMark> _marks = [];

    /// <inheritdoc />
    protected override string RootClass => "veto-slider";

    /// <summary>The low end of the scale.</summary>
    [Parameter]
    public TValue Min { get; set; } = TValue.Zero;

    /// <summary>The high end of the scale.</summary>
    [Parameter]
    public TValue Max { get; set; } = TValue.CreateSaturating(100);

    /// <summary>The smallest change a drag or an arrow key can make.</summary>
    [Parameter]
    public TValue Step { get; set; } = TValue.One;

    /// <summary>
    /// When true the slider has two thumbs and picks a span: <c>Value</c> is the low end and
    /// <see cref="UpperValue"/> the high end.
    /// </summary>
    [Parameter]
    public bool Range { get; set; }

    /// <summary>The high end of the selected span. Use <c>@bind-UpperValue</c>. Ignored unless <see cref="Range"/> is set.</summary>
    [Parameter]
    public TValue UpperValue { get; set; } = TValue.CreateSaturating(100);

    /// <summary>Raised when the high end changes, so <c>@bind-UpperValue</c> works.</summary>
    [Parameter]
    public EventCallback<TValue> UpperValueChanged { get; set; }

    /// <summary>
    /// The notches on the rail. Each carries a position on the slider's own scale and,
    /// optionally, a label - a number for a plain scale, a word where the number means nothing
    /// to the reader. Supplying this overrides <see cref="ShowMarks"/>.
    /// </summary>
    [Parameter]
    public IReadOnlyList<VetoSliderMark>? Marks { get; set; }

    /// <summary>
    /// When true and no <see cref="Marks"/> are supplied, lays out a labelled notch every
    /// <see cref="MarkEvery"/> - or every <see cref="Step"/> when that is not set.
    /// </summary>
    [Parameter]
    public bool ShowMarks { get; set; }

    /// <summary>The spacing of generated marks. Defaults to <see cref="Step"/>.</summary>
    [Parameter]
    public double? MarkEvery { get; set; }

    /// <summary>When false the notches are drawn but their labels are not.</summary>
    [Parameter]
    public bool ShowMarkLabels { get; set; } = true;

    /// <summary>
    /// When true a thumb settles on the nearest mark instead of anywhere on the scale, turning
    /// a continuous slider into a chooser between named stops.
    /// </summary>
    [Parameter]
    public bool SnapToMarks { get; set; }

    /// <summary>When true the current value is printed above the rail.</summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>Standard or custom numeric format for every number the slider prints, e.g. <c>N0</c> or <c>0.##</c>.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Text put in front of a printed value, such as a currency symbol.</summary>
    [Parameter]
    public string? Prefix { get; set; }

    /// <summary>Text put after a printed value, such as <c>%</c> or <c>kg</c>.</summary>
    [Parameter]
    public string? Suffix { get; set; }

    /// <summary>What goes between the two numbers when a range prints its value.</summary>
    [Parameter]
    public string RangeSeparator { get; set; } = " – ";

    /// <summary>Accessible name for the low thumb of a range.</summary>
    [Parameter]
    public string LowerThumbLabel { get; set; } = "Minimum";

    /// <summary>Accessible name for the high thumb of a range.</summary>
    [Parameter]
    public string UpperThumbLabel { get; set; } = "Maximum";

    /// <summary>The accent the filled part of the rail takes.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>
    /// When true - the default - the bound value follows the thumb as it is dragged. Set it
    /// false to update only when the drag ends.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; } = true;

    /// <summary>
    /// A bubble over each thumb carrying its value, formatted like <see cref="ShowValue"/>.
    /// <see cref="VetoSliderThumbValue.Active"/> shows it while the thumb is hovered, focused or
    /// dragged; <see cref="VetoSliderThumbValue.Always"/> keeps it up.
    /// </summary>
    [Parameter]
    public VetoSliderThumbValue ThumbValue { get; set; } = VetoSliderThumbValue.Never;

    /// <summary>Raised after the bound value changes, with the new value.</summary>
    [Parameter]
    public EventCallback<TValue> OnValueChanged { get; set; }

    /// <summary>The low end of the selection as a number.</summary>
    private double LowerNumber => ToNumber(CurrentValue);

    /// <summary>The high end of the selection as a number; equal to the low end outside range mode.</summary>
    private double UpperNumber => Range ? ToNumber(UpperValue) : LowerNumber;

    private double MinNumber => ToNumber(Min);

    private double MaxNumber => ToNumber(Max);

    private double StepNumber
    {
        get
        {
            var step = ToNumber(Step);
            return step > 0 ? step : 1;
        }
    }

    private double Span
    {
        get
        {
            var span = MaxNumber - MinNumber;
            return span > 0 ? span : 1;
        }
    }

    private IReadOnlyList<VetoSliderMark> VisibleMarks => _marks;

    private bool HasMarkLabels => _marks.Any(m => m.Label is not null);

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    private string UpperElementId => $"{ElementId}-upper";

    private string AccessibleName => Label ?? "Value";

    private string? LowerAriaLabel => Range ? LowerThumbLabel : HasLabel ? null : AccessibleName;

    /// <summary>The fill's extent, handed to the CSS so the rail does not need an inline width per element.</summary>
    private string ControlStyle
        => $"--veto-slider-start:{PercentCss(Range ? LowerNumber : MinNumber)}%;--veto-slider-end:{PercentCss(UpperNumber)}%";

    private string ValueText
        => Range
            ? $"{TextFor(LowerNumber)}{RangeSeparator}{TextFor(UpperNumber)}"
            : TextFor(LowerNumber);

    /// <summary>
    /// The <c>step</c> attribute.
    /// <para>
    /// Snapping needs the browser's own quantisation out of the way, so the slider asks for a
    /// continuous scale and rounds to the nearest mark itself. Evenly spaced marks are the
    /// exception: handing the browser that spacing as the step means an arrow key walks from
    /// one mark to the next by itself, which <c>any</c> - a hundredth of the scale per press -
    /// would not.
    /// </para>
    /// </summary>
    private string StepAttribute
    {
        get
        {
            if (!SnapToMarks || _marks.Count == 0)
            {
                return AttrNumber(StepNumber);
            }

            var spacing = EvenMarkSpacing();

            return spacing is null ? "any" : AttrNumber(spacing.Value);
        }
    }

    /// <summary>The gap between marks when every gap is the same, otherwise null.</summary>
    private double? EvenMarkSpacing ()
    {
        if (_marks.Count < 2)
        {
            return null;
        }

        var sorted = _marks.Select(m => m.Value).Order().ToArray();
        var spacing = sorted[1] - sorted[0];

        if (spacing <= 0)
        {
            return null;
        }

        for (var i = 2; i < sorted.Length; i++)
        {
            if (Math.Abs(sorted[i] - sorted[i - 1] - spacing) > spacing * 1e-6)
            {
                return null;
            }
        }

        return spacing;
    }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();
        _marks = BuildMarks();
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("range"), Range)
            .AddClass(Modifier("marked"), _marks.Count > 0)
            .AddClass(Modifier("mark-labels"), ShowMarkLabels && HasMarkLabels)
            .AddClass(Modifier("bubble-always"), ThumbValue == VetoSliderThumbValue.Always)
            .AddClass(Modifier("lower-on-top"), LowerOnTop);

    /// <summary>
    /// Whether the low thumb must sit above the high one. The inputs overlap, and normally the
    /// high one is on top - but when both thumbs meet in the upper half, the one on top could
    /// only move right, which it cannot, and the slider would be stuck. There the low thumb,
    /// which can move left, has to be the one the pointer finds.
    /// </summary>
    private bool LowerOnTop => Range && LowerNumber >= UpperNumber && LowerNumber > MinNumber + Span / 2;

    private IReadOnlyList<VetoSliderMark> BuildMarks ()
    {
        if (Marks is { Count: > 0 })
        {
            return [.. Marks.Where(m => m.Value >= MinNumber && m.Value <= MaxNumber)];
        }

        if (!ShowMarks)
        {
            return [];
        }

        var every = MarkEvery ?? StepNumber;

        if (every <= 0)
        {
            return [];
        }

        var generated = new List<VetoSliderMark>();

        // The epsilon keeps the last mark from being lost to floating-point drift when the span
        // divides evenly - without it a 0-100 scale every 12.5 stops at 87.5.
        for (var value = MinNumber; value <= MaxNumber + every * 1e-9; value += every)
        {
            generated.Add(new VetoSliderMark(Math.Min(value, MaxNumber), FormatNumber(Math.Min(value, MaxNumber))));

            // A pathological Min/Max/MarkEvery combination must not hang the render loop.
            if (generated.Count > 200)
            {
                break;
            }
        }

        return generated;
    }

    private double Percent ( double value ) => Math.Round((value - MinNumber) / Span * 100, 4);

    /// <summary>
    /// A percentage for a CSS declaration. Always invariant: a culture that writes decimals with
    /// a comma would otherwise emit <c>left:33,3333%</c>, which CSS silently drops.
    /// </summary>
    private string PercentCss ( double value ) => Percent(value).ToString("0.####", Inv);

    private bool IsSelected ( double value )
        => Range ? value >= LowerNumber && value <= UpperNumber : value <= LowerNumber;

    private static double ToNumber ( TValue value ) => double.CreateSaturating(value);

    private static TValue FromNumber ( double value ) => TValue.CreateSaturating(value);

    private string FormatNumber ( double value )
        => Format is null ? value.ToString("0.##########", Inv) : value.ToString(Format, CultureInfo.CurrentCulture);

    /// <summary>
    /// A number for an HTML attribute - <c>min</c>, <c>max</c>, <c>step</c>, <c>data-value</c>.
    /// Never <see cref="Format"/> or the current culture: <c>max="5,000"</c> or <c>step="0,1"</c>
    /// is not a number to the browser, which then quietly falls back to its defaults.
    /// </summary>
    private static string AttrNumber ( double value ) => value.ToString("0.##########", Inv);

    private string TextFor ( double value ) => $"{Prefix}{FormatNumber(value)}{Suffix}";

    /// <summary>
    /// The rendered <c>value</c> attribute, in one of two equivalent formats. See
    /// <see cref="_resync"/> for why the format alternates.
    /// </summary>
    private string ValueAttribute ( double value )
        => _resync ? value.ToString("0.0000000", Inv) : value.ToString("0.##########", Inv);

    private Task OnLowerInputAsync ( ChangeEventArgs args )
        => Immediate ? ApplyLowerAsync(args) : Task.CompletedTask;

    private Task OnLowerChangeAsync ( ChangeEventArgs args )
        => Immediate ? Task.CompletedTask : ApplyLowerAsync(args);

    private Task OnUpperInputAsync ( ChangeEventArgs args )
        => Immediate ? ApplyUpperAsync(args) : Task.CompletedTask;

    private Task OnUpperChangeAsync ( ChangeEventArgs args )
        => Immediate ? Task.CompletedTask : ApplyUpperAsync(args);

    private async Task ApplyLowerAsync ( ChangeEventArgs args )
    {
        if (!TryRead(args, out var raw))
        {
            return;
        }

        var next = Normalize(raw);

        // The thumbs may meet but not pass each other.
        if (Range && next > UpperNumber)
        {
            next = UpperNumber;
        }

        if (ReadOnly || next != raw)
        {
            _resync = !_resync;
        }

        if (ReadOnly || next == LowerNumber)
        {
            return;
        }

        CurrentValue = FromNumber(next);

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }
    }

    private async Task ApplyUpperAsync ( ChangeEventArgs args )
    {
        if (!Range || !TryRead(args, out var raw))
        {
            return;
        }

        var next = Normalize(raw);

        if (next < LowerNumber)
        {
            next = LowerNumber;
        }

        if (ReadOnly || next != raw)
        {
            _resync = !_resync;
        }

        if (ReadOnly || next == UpperNumber)
        {
            return;
        }

        UpperValue = FromNumber(next);
        await UpperValueChanged.InvokeAsync(UpperValue);
    }

    private static bool TryRead ( ChangeEventArgs args, out double value )
        => double.TryParse(args.Value?.ToString(), NumberStyles.Float, Inv, out value);

    private double Normalize ( double value )
    {
        var clamped = Math.Clamp(value, MinNumber, MaxNumber);

        if (!SnapToMarks || _marks.Count == 0)
        {
            return clamped;
        }

        var nearest = _marks[0].Value;

        foreach (var mark in _marks)
        {
            if (Math.Abs(mark.Value - clamped) < Math.Abs(nearest - clamped))
            {
                nearest = mark.Value;
            }
        }

        return nearest;
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString ( TValue value ) => ToNumber(value).ToString("0.##########", Inv);

    /// <inheritdoc />
    protected override bool TryParseValueFromString (
        string? value,
        [MaybeNullWhen( false )] out TValue result,
        [NotNullWhen( false )] out string? validationErrorMessage )
    {
        if (double.TryParse(value, NumberStyles.Float, Inv, out var parsed))
        {
            result = FromNumber(parsed);
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }
}
