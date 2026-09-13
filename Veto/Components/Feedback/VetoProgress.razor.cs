using Microsoft.AspNetCore.Components;
using System.Globalization;
using Veto.Core;

namespace Veto.Components.Feedback;

/// <summary>
/// A horizontal progress bar. Determinate when you can say how far along the work is,
/// indeterminate when you cannot.
/// </summary>
/// <remarks>
/// Prefer a determinate bar wherever a total is known: a bar that fills tells someone how long
/// to wait, where a spinner only tells them to.
/// </remarks>
public partial class VetoProgress : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-progress";

    /// <summary>How far along the work is, from 0 to <see cref="Max"/>.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The value that counts as finished.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Animates continuously instead of filling, for work with no known total.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>Text above the bar saying what is happening.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Shows the percentage beside the label. Ignored when indeterminate.</summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>The bar's thickness.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>Rounds the ends of the bar. True by default.</summary>
    [Parameter]
    public bool Rounded { get; set; } = true;

    /// <summary>
    /// The accessible name used when no <see cref="Label"/> is given. A progress bar with no
    /// name is announced as a bare number.
    /// </summary>
    [Parameter]
    public string AccessibleLabel { get; set; } = "Progress";

    /// <summary>The value as a percentage of <see cref="Max"/>, clamped to 0-100.</summary>
    private double Percent => Max <= 0 ? 0 : Math.Clamp(Value / Max * 100, 0, 100);

    private string ValueText => Percent.ToString("0", CultureInfo.InvariantCulture) + "%";

    private string? FillStyle => !Indeterminate
        ? string.Create(CultureInfo.InvariantCulture, $"width: {Percent:0.##}%")
        : null;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("indeterminate"), Indeterminate)
            .AddClass(Modifier("rounded"), Rounded);
}