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

    private bool IsDecorative => string.IsNullOrWhiteSpace(Title);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToString().ToLowerInvariant()))
            .AddClass(Modifier("spin"), Spin);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-icon-rotation", $"{Rotation}deg", Rotation != 0);
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
