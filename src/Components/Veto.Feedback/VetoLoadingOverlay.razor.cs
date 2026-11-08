using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Feedback;

/// <summary>
/// Veils its content while something is refreshing, keeping what is already there visible
/// underneath.
/// </summary>
/// <remarks>
/// <para>
/// For refreshing data that is already on screen. Use <c>VetoSkeleton</c> instead for the first
/// load, when there is nothing to veil.
/// </para>
/// <para>
/// The content is marked <c>inert</c> while loading, so it cannot be clicked or tabbed into
/// behind the veil - a control that is visible but about to be replaced should not be operable.
/// </para>
/// </remarks>
public partial class VetoLoadingOverlay : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-loading-overlay";

    /// <summary>The content being veiled.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the veil is showing.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>What is being waited for. Announced, and shown beside the spinner.</summary>
    [Parameter]
    public string Label { get; set; } = "Loading";

    /// <summary>Replaces the default spinner and label.</summary>
    [Parameter]
    public RenderFragment? Indicator { get; set; }

    /// <summary>
    /// How long, in milliseconds, the veil waits before it fades in. A refresh that finishes
    /// sooner never shows it, so a fast response does not flash. The content is inert from the
    /// start either way. 0, the default, shows it at once.
    /// </summary>
    [Parameter]
    public int ShowDelay { get; set; }

    /// <summary>Whether the content is dimmed as well as veiled.</summary>
    [Parameter]
    public bool Dim { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("loading"), Loading)
            .AddClass(Modifier("dim"), Dim)
            .AddClass(Modifier("delayed"), ShowDelay > 0);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-loading-overlay-delay", string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{ShowDelay}ms"), ShowDelay > 0);
}