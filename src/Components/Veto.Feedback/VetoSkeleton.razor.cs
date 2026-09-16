using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Feedback;

/// <summary>The shape a <see cref="VetoSkeleton"/> stands in for.</summary>
public enum VetoSkeletonVariant
{
    /// <summary>Lines of text.</summary>
    Text,

    /// <summary>A block - an image, a card, a chart.</summary>
    Rectangle,

    /// <summary>A circle - an avatar, an icon.</summary>
    Circle
}

/// <summary>
/// A placeholder in the shape of the content that is coming, for the wait before anything has
/// arrived.
/// </summary>
/// <remarks>
/// <para>
/// Hidden from assistive technology: a screen reader user gains nothing from being told about
/// grey boxes. Announce the wait once, on the region that is loading, with <c>aria-busy</c> and
/// a label - not on every placeholder.
/// </para>
/// <para>
/// Worth using only where the skeleton resembles what replaces it. A placeholder that does not
/// match makes the arrival of the real content feel like a jump.
/// </para>
/// </remarks>
public partial class VetoSkeleton : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-skeleton";

    /// <summary>The shape being stood in for.</summary>
    [Parameter]
    public VetoSkeletonVariant Variant { get; set; } = VetoSkeletonVariant.Text;

    /// <summary>How many lines to draw. Only applies to <see cref="VetoSkeletonVariant.Text"/>.</summary>
    [Parameter]
    public int Lines { get; set; } = 1;

    /// <summary>The placeholder's width, as a CSS length.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>The placeholder's height, as a CSS length. Text and circles size themselves.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Whether the shimmer runs. Ignored under reduced motion, which always stills it.</summary>
    [Parameter]
    public bool Animated { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Variant.ToString().ToLowerInvariant()))
            .AddClass(Modifier("animated"), Animated)
            .AddClass(Modifier("multiline"), Variant == VetoSkeletonVariant.Text && Lines > 1);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("width", Width)
            .AddStyle("height", Height);
}