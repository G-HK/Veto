using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// Constrains content to a comfortable reading width and centres it in the space available.
/// Wrap a page's content in one of these rather than letting text run the full width of a
/// wide display.
/// </summary>
public partial class VetoContainer : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-container";

    /// <summary>The page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>How wide the content is allowed to get.</summary>
    [Parameter]
    public VetoContainerSize Size { get; set; } = VetoContainerSize.Medium;

    /// <summary>Horizontal padding, so content never touches the viewport edge on small screens.</summary>
    [Parameter]
    public VetoSpacing Padding { get; set; } = VetoSpacing.Large;

    /// <summary>Whether the container is centred in the space available. True by default.</summary>
    [Parameter]
    public bool Centered { get; set; } = true;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToString().ToLowerInvariant()))
            .AddClass(Modifier($"padding-{Padding.ToModifier()}"))
            .AddClass(Modifier("centered"), Centered);
}

/// <summary>How wide a <see cref="VetoContainer"/> lets its content grow.</summary>
public enum VetoContainerSize
{
    /// <summary>30rem - a single column of text, a sign-in form.</summary>
    Small,

    /// <summary>48rem - prose and documentation. The default.</summary>
    Medium,

    /// <summary>64rem - content with a sidebar or wide tables.</summary>
    Large,

    /// <summary>80rem - dashboards.</summary>
    ExtraLarge,

    /// <summary>No limit; padding still applies.</summary>
    Full
}
