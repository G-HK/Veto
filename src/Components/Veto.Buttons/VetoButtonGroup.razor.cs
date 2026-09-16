using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// A cluster of related buttons that reads and behaves as one control.
/// </summary>
/// <remarks>
/// <para>
/// Attached by default: the buttons share their touching borders and only the outer corners of
/// the group are rounded, so the cluster looks like one segmented control rather than several
/// buttons that happen to be adjacent. Set <see cref="Attached"/> false for a spaced row.
/// </para>
/// <para>
/// <see cref="Size"/> is set once on the group and normalises every button inside it, so a
/// group cannot end up with uneven heights because one button was left at the default. Setting
/// <c>Size</c> on the individual buttons as well is still fine - and is what you want when a
/// group is built from a mix of <see cref="VetoButton"/> and <see cref="VetoIconButton"/> - but
/// it is no longer necessary.
/// </para>
/// <para>
/// Separators: <see cref="Separators"/> draws a line between every child at once. For a line
/// between only some of them, leave it off and place <see cref="VetoButtonGroupSeparator"/>
/// where you want it - which is how a toolbar clusters two or three actions, then breaks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoButtonGroup Size="VetoSize.Small" Label="Text alignment"&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.AlignLeft" Label="Left" /&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.AlignCenter" Label="Centre" /&gt;
///     &lt;VetoButtonGroupSeparator /&gt;
///     &lt;VetoButtonGroupText&gt;Indent&lt;/VetoButtonGroupText&gt;
/// &lt;/VetoButtonGroup&gt;
/// </code>
/// </example>
public partial class VetoButtonGroup : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-button-group";

    /// <summary>The buttons, separators and labels in the group.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the group runs across or down.</summary>
    [Parameter]
    public VetoOrientation Orientation { get; set; } = VetoOrientation.Horizontal;

    /// <summary>
    /// The height every button in the group takes. Applied to the group rather than to each
    /// button, so the row cannot come out uneven.
    /// </summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>
    /// When true - the default - the buttons share their touching borders and only the group's
    /// outer corners are rounded. False leaves them as separate buttons with a gap.
    /// </summary>
    [Parameter]
    public bool Attached { get; set; } = true;

    /// <summary>
    /// Draws a line between every child. For a line between only some of them, leave this off
    /// and place <see cref="VetoButtonGroupSeparator"/> by hand.
    /// </summary>
    [Parameter]
    public bool Separators { get; set; }

    /// <summary>When true the group stretches to fill its container and the buttons share the width evenly.</summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>The group's accessible name. Give one whenever the buttons alone do not say what the cluster is for.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// When true the group reports itself as an ARIA <c>toolbar</c> rather than a <c>group</c>.
    /// Use it for the outermost container of a real toolbar; a toolbar should not contain
    /// another toolbar.
    /// </summary>
    [Parameter]
    public bool IsToolbar { get; set; }

    private string Role => IsToolbar ? "toolbar" : "group";

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Orientation.ToModifier()))
            .AddClass(Modifier("attached"), Attached)
            .AddClass(Modifier("separated"), Separators)
            .AddClass(Modifier("full-width"), FullWidth)
            .AddClass(Modifier("toolbar"), IsToolbar);
}