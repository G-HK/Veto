using System.Globalization;
using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Icon;

/// <summary>
/// Renders an SVG icon that inherits its colour and size from context. Pass a constant from
/// <see cref="VetoIcons"/>, your own SVG markup, or SVG elements as child content.
/// </summary>
/// <remarks>
/// Icons are decorative by default - hidden from assistive technology - because they usually
/// sit beside a label that already says what they mean. Give a <see cref="Title"/> only when
/// the icon is the sole carrier of meaning, such as an icon-only button.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoIcon Icon="@VetoIcons.Search" /&gt;
/// &lt;VetoIcon Icon="@VetoIcons.Trash" Title="Delete" Size="VetoIconSize.Large" /&gt;
/// </code>
/// </example>
public partial class VetoIcon : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-icon";

    /// <summary>
    /// The icon's SVG inner markup, normally a constant from <see cref="VetoIcons"/>. Any SVG
    /// markup works as long as it draws with <c>currentColor</c>.
    /// </summary>
    /// <remarks>
    /// Rendered as raw markup, so it must never be built from user input - treat it the way
    /// you would treat any other HTML you inject.
    /// </remarks>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>SVG elements to render instead of <see cref="Icon"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The rendered size.</summary>
    [Parameter]
    public VetoIconSize Size { get; set; } = VetoIconSize.Medium;

    /// <summary>
    /// The icon's accessible name. Supply it only when the icon carries meaning no nearby text
    /// already conveys; leaving it null keeps the icon decorative and out of the accessibility tree.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Stroke width, for matching the weight of surrounding text.</summary>
    [Parameter]
    public double StrokeWidth { get; set; } = 2;

    /// <summary>The SVG viewBox. Only needs changing for icons not drawn on a 24x24 grid.</summary>
    [Parameter]
    public string ViewBox { get; set; } = "0 0 24 24";

    /// <summary>Rotation in degrees - reuses one chevron for all four directions.</summary>
    [Parameter]
    public int Rotation { get; set; }

    /// <summary>Spins the icon continuously, for loading states. Ignored under reduced motion.</summary>
    [Parameter]
    public bool Spin { get; set; }

    /// <summary>
    /// A theme colour for the icon. Null - the default - inherits the colour of the text around
    /// it, which is what an icon beside a label wants.
    /// </summary>
    [Parameter]
    public VetoColor? Color { get; set; }

    /// <summary>Mirrors the icon - one arrow drawn once serves as its own reflection.</summary>
    [Parameter]
    public VetoIconFlip Flip { get; set; } = VetoIconFlip.None;

    private bool IsDecorative => string.IsNullOrWhiteSpace(Title);

    // SVG wants "1.5". Written through the current culture a German or French browser got
    // "1,5", which is not a number, and the stroke fell back to its default width.
    private string StrokeWidthAttribute => StrokeWidth.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToString().ToLowerInvariant()))
            .AddClass(Modifier("spin"), Spin)
            .AddClass(Modifier(Color?.ToModifier() ?? string.Empty), Color is not null)
            .AddClass(Modifier($"flip-{Flip.ToString().ToLowerInvariant()}"), Flip != VetoIconFlip.None);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            // Invariant: some cultures (sv-SE, nb-NO) write a negative number with U+2212, not "-".
            .AddStyle("--veto-icon-rotation", string.Create(CultureInfo.InvariantCulture, $"{Rotation}deg"), Rotation != 0);
}

/// <summary>Which way a <see cref="VetoIcon"/> is mirrored.</summary>
public enum VetoIconFlip
{
    /// <summary>Drawn as designed.</summary>
    None,

    /// <summary>Mirrored left to right.</summary>
    Horizontal,

    /// <summary>Mirrored top to bottom.</summary>
    Vertical,

    /// <summary>Mirrored both ways - the same as a half turn.</summary>
    Both
}

/// <summary>How large a <see cref="VetoIcon"/> renders.</summary>
public enum VetoIconSize
{
    /// <summary>0.875rem - inline with small text.</summary>
    Small,

    /// <summary>1rem - the default, matching body text.</summary>
    Medium,

    /// <summary>1.5rem - buttons and nav items.</summary>
    Large,

    /// <summary>2rem - dock items and empty states.</summary>
    ExtraLarge,

    /// <summary>Inherits the current font size, so the icon scales with its text.</summary>
    Inherit
}
