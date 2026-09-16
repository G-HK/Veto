using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Rating;

/// <summary>
/// A row of icons the user picks a score from - stars out of the box, or any other icon.
/// </summary>
/// <remarks>
/// <para>
/// Halves are drawn by clipping a filled copy of the icon over an empty one, so
/// <see cref="AllowHalf"/> works for every icon rather than only for the handful that ship a
/// purpose-drawn half glyph.
/// </para>
/// <para>
/// The row is one focus stop, not <see cref="Max"/> of them: it reports itself as an ARIA
/// slider, so assistive technology announces "3 of 6" once, and the arrow keys walk the scale
/// - a half step at a time when halves are allowed. The per-icon hit targets are taken out of
/// the tab order for the same reason.
/// </para>
/// </remarks>
/// <typeparam name="TValue">
/// Any number type. Use a fractional one - <c>double</c>, <c>decimal</c>, <c>float</c> - with
/// <see cref="AllowHalf"/>; an integer type has nowhere to hold the half and is rejected.
/// </typeparam>
/// <example>
/// <code>
/// &lt;VetoRating TValue="int" Label="Rating" @bind-Value="_score" /&gt;
/// &lt;VetoRating TValue="double" AllowHalf="true" Icon="@VetoIcons.Heart" @bind-Value="_score" /&gt;
/// </code>
/// </example>
public partial class VetoRating<TValue> : VetoInputBase<TValue>
    where TValue : struct, INumber<TValue>
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>True when <typeparamref name="TValue"/> can hold a half, which is what halves need.</summary>
    private static readonly bool SupportsHalves = double.CreateSaturating(TValue.CreateSaturating(0.5)) == 0.5;

    private double? _preview;

    /// <inheritdoc />
    protected override string RootClass => "veto-rating";

    /// <summary>How many icons the row holds, and so the highest score. Six by default.</summary>
    [Parameter]
    public int Max { get; set; } = 6;

    /// <summary>
    /// The icon the row is drawn from, normally a constant from <see cref="VetoIcons"/>.
    /// Defaults to a star.
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = VetoIcons.Star;

    /// <summary>
    /// The icon drawn for the unearned part of the row. Defaults to <see cref="Icon"/>, which
    /// is what you want when the same shape reads as full or empty by colour alone; set it to
    /// draw a different outline for the empty slots.
    /// </summary>
    [Parameter]
    public string? EmptyIcon { get; set; }

    /// <summary>
    /// When true a score may land on a half - 3.5 out of 6. Requires a fractional
    /// <typeparamref name="TValue"/>.
    /// </summary>
    [Parameter]
    public bool AllowHalf { get; set; }

    /// <summary>
    /// When true - the default - picking the score the rating already holds clears it back to
    /// zero, which is the only way to undo a rating with the pointer alone.
    /// </summary>
    [Parameter]
    public bool Clearable { get; set; } = true;

    /// <summary>When true - the default - the row previews the score under the pointer before it is picked.</summary>
    [Parameter]
    public bool HoverPreview { get; set; } = true;

    /// <summary>When true the score is printed after the row.</summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>
    /// A name per score, from one upwards - <c>["Terrible", "Poor", ..]</c>. Used for each
    /// icon's tooltip and accessible name, and for the announced value. Shorter than
    /// <see cref="Max"/> is fine; the rest fall back to the number.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? ItemLabels { get; set; }

    /// <summary>The colour the earned icons take.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Warning;

    /// <summary>Raised after the score changes, with the new value.</summary>
    [Parameter]
    public EventCallback<TValue> OnValueChanged { get; set; }

    /// <summary>The committed score - what the row is actually bound to, ignoring any hover preview.</summary>
    private double Score => ToNumber(CurrentValue);

    /// <summary>The score currently drawn - the previewed one while the pointer is over the row.</summary>
    private double Number => _preview ?? Score;

    /// <summary>The smallest change: a half when halves are allowed, otherwise a whole icon.</summary>
    private double StepSize => AllowHalf ? 0.5 : 1;

    private bool Interactive => !Disabled && !ReadOnly;

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    private string AccessibleName => Label ?? "Rating";

    private string ValueText
    {
        get
        {
            var value = Score;
            var named = NameFor(value);

            return named is null
                ? $"{FormatNumber(value)} of {Max}"
                : $"{FormatNumber(value)} of {Max}, {named}";
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (Max < 1)
        {
            throw new InvalidOperationException($"{nameof(Max)} must be at least 1, but was {Max}.");
        }

        if (AllowHalf && !SupportsHalves)
        {
            throw new InvalidOperationException(
                $"{nameof(AllowHalf)} needs a fractional TValue such as double or decimal; {typeof(TValue)} cannot hold a half.");
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("half"), AllowHalf)
            .AddClass(Modifier("interactive"), Interactive)
            .AddClass(Modifier("previewing"), _preview is not null);

    /// <summary>How much of one icon is earned: <c>full</c>, <c>half</c> or <c>empty</c>.</summary>
    private string FillOf ( int index ) => FillPercent(index) switch
    {
        >= 100 => "full",
        <= 0 => "empty",
        _ => "half"
    };

    /// <summary>How much of one icon is filled, as a percentage of its width.</summary>
    private double FillPercent ( int index ) => Math.Clamp((Number - (index - 1)) * 100, 0, 100);

    /// <summary>
    /// The same figure as a CSS width. Always invariant: a culture that writes decimals with a
    /// comma would otherwise emit <c>width:66,67%</c>, which CSS silently drops.
    /// </summary>
    private string FillCss ( int index ) => FillPercent(index).ToString("0.####", Inv);

    private string? NameFor ( double value )
    {
        if (ItemLabels is null || value <= 0)
        {
            return null;
        }

        var index = (int)Math.Ceiling(value) - 1;

        return index >= 0 && index < ItemLabels.Count ? ItemLabels[index] : null;
    }

    private string TitleFor ( double value )
    {
        var named = NameFor(value);

        return named is null ? $"{FormatNumber(value)} of {Max}" : $"{FormatNumber(value)} of {Max}, {named}";
    }

    private static double ToNumber ( TValue value ) => double.CreateSaturating(value);

    private static TValue FromNumber ( double value ) => TValue.CreateSaturating(value);

    private static string FormatNumber ( double value ) => value.ToString("0.##", Inv);

    private Task PreviewAsync ( double value )
    {
        if (Interactive && HoverPreview)
        {
            _preview = value;
        }

        return Task.CompletedTask;
    }

    private void ClearPreview () => _preview = null;

    private Task PickAsync ( double value )
    {
        _preview = null;

        // Picking the score it already holds clears it, so a mis-click is undoable without
        // reaching for the keyboard.
        var next = Clearable && value == ToNumber(CurrentValue) ? 0 : value;

        return SetValueAsync(next);
    }

    /// <summary>Sets the score, clamped to the row, honouring <c>Disabled</c> and <c>ReadOnly</c>.</summary>
    /// <param name="value">The new score, between zero and <see cref="Max"/>.</param>
    public async Task SetValueAsync ( double value )
    {
        if (!Interactive)
        {
            return;
        }

        var next = Math.Clamp(value, 0, Max);

        if (!AllowHalf)
        {
            next = Math.Round(next, MidpointRounding.AwayFromZero);
        }

        if (next == ToNumber(CurrentValue))
        {
            return;
        }

        CurrentValue = FromNumber(next);

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }
    }

    private Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        // Only keys the browser does not already spend on scrolling or focus are bound, so the
        // row never has to swallow a keystroke it does not own - no preventDefault, no Tab trap.
        if (args.Key is "ArrowRight" or "ArrowUp")
        {
            return SetValueAsync(ToNumber(CurrentValue) + StepSize);
        }

        if (args.Key is "ArrowLeft" or "ArrowDown")
        {
            return SetValueAsync(ToNumber(CurrentValue) - StepSize);
        }

        if (args.Key is "Delete" or "Backspace")
        {
            return SetValueAsync(0);
        }

        if (args.Key.Length == 1 && char.IsAsciiDigit(args.Key[0]))
        {
            return SetValueAsync(args.Key[0] - '0');
        }

        return Task.CompletedTask;
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
