using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Veto.Core;

namespace Veto.Components.Navigation;

/// <summary>
/// A text link that starts bare and reveals a thin neon underline, growing from the middle, when
/// the pointer or the keyboard reaches it.
/// </summary>
/// <remarks>
/// <para>
/// The reveal answers <c>:focus-visible</c> as well as <c>:hover</c>. A link that underlined
/// only on hover would leave someone tabbing through the page with nothing but the focus ring to
/// go on, and the whole point of the effect is to say "this one".
/// </para>
/// <para>
/// It also knows whether it points at the page currently being shown, marking itself with
/// <c>aria-current="page"</c> and a modifier class - so the same component serves as a nav link
/// without a second one existing to do that job.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoLink Href="/docs/installation"&gt;Installation&lt;/VetoLink&gt;
/// &lt;VetoLink Href="https://example.com" Underline="VetoUnderline.Always"&gt;Elsewhere&lt;/VetoLink&gt;
/// </code>
/// </example>
public partial class VetoLink : VetoComponentBase, IDisposable
{
    private string _currentPath = string.Empty;

    /// <inheritdoc />
    protected override string RootClass => "veto-link";

    /// <summary>The link's text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the link goes.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>How the underline behaves.</summary>
    [Parameter]
    public VetoUnderline Underline { get; set; } = VetoUnderline.Reveal;

    /// <summary>
    /// The underline's colour - any CSS colour. Defaults to the theme's accent.
    /// </summary>
    [Parameter]
    public string? UnderlineColor { get; set; }

    /// <summary>Takes its colour from the surrounding text instead of the accent.</summary>
    /// <remarks>For a link inside a paragraph, where an accent-coloured run of words is noise.</remarks>
    [Parameter]
    public bool Inherit { get; set; }

    /// <summary>The link's <c>target</c>.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>
    /// The link's <c>rel</c>. Left alone a link opening in a new tab is given
    /// <c>noopener noreferrer</c>, because the page it opens can otherwise reach back through
    /// <c>window.opener</c>.
    /// </summary>
    [Parameter]
    public string? Rel { get; set; }

    /// <summary>
    /// Matches the current URL exactly rather than as a prefix. Needed on a link to "/", which
    /// every other path would otherwise start with.
    /// </summary>
    [Parameter]
    public bool MatchExact { get; set; }

    /// <summary>True when this link points at the page currently being shown.</summary>
    public bool IsActive => VetoRouteMatch.IsActive(_currentPath, Href, MatchExact);

    private string? ResolvedRel
        => Rel ?? (Target == "_blank" ? "noopener noreferrer" : null);

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        _currentPath = VetoRouteMatch.CurrentPath(Navigation);
        Navigation.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Underline.ToUnderlineClass())
            .AddClass(Modifier("inherit"), Inherit)
            .AddClass(Modifier("active"), IsActive);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-underline-color", VetoEffects.SafeColor(UnderlineColor));

    private void OnLocationChanged ( object? sender, LocationChangedEventArgs args )
    {
        _currentPath = VetoRouteMatch.CurrentPath(Navigation);
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose () => Navigation.LocationChanged -= OnLocationChanged;
}
