using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Feedback;

/// <summary>
/// A spinner for work whose duration is unknown. Use <see cref="VetoProgress"/> instead when
/// you can say how far along it is - a spinner tells someone to wait, a bar tells them how long.
/// </summary>
/// <remarks>
/// Decorative by default. Give it a <see cref="Label"/> only when it is the sole indication
/// that something is happening; when it sits beside text that already says "Loading…", a second
/// announcement is noise.
/// </remarks>
public partial class VetoSpinner : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-spinner";

    /// <summary>The spinner's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The colour role. Inherits the surrounding text colour by default.</summary>
    [Parameter]
    public VetoColor? Color { get; set; }

    /// <summary>
    /// What is being waited for, announced to assistive technology. Leaving it null keeps the
    /// spinner decorative and silent.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The stroke's thickness, as a CSS length.</summary>
    [Parameter]
    public string Thickness { get; set; } = "2px";

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(() => Modifier(Color!.Value.ToModifier()), Color.HasValue);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-spinner-thickness", Thickness);
}