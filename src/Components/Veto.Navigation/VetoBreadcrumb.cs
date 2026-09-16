using Microsoft.AspNetCore.Components;

namespace Veto.Components.Navigation;

/// <summary>
/// One step of a <see cref="VetoBreadcrumbs"/> trail, written as a child rather than passed in
/// a list.
/// </summary>
/// <remarks>
/// Draws nothing itself: it registers with the enclosing trail, which lays every step out
/// together so the separators, the collapsing and the <c>aria-current</c> on the last step are
/// decided in one place. Give it <see cref="ChildContent"/> to put arbitrary markup in a step -
/// a badge, a count, an icon of your own - which is the reason to reach for this form over the
/// <c>Items</c> list.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoBreadcrumbs&gt;
///     &lt;VetoBreadcrumb Href="/" Icon="@VetoIcons.Home"&gt;Home&lt;/VetoBreadcrumb&gt;
///     &lt;VetoBreadcrumb Href="/docs"&gt;Docs&lt;/VetoBreadcrumb&gt;
///     &lt;VetoBreadcrumb&gt;Breadcrumbs&lt;/VetoBreadcrumb&gt;
/// &lt;/VetoBreadcrumbs&gt;
/// </code>
/// </example>
public class VetoBreadcrumb : ComponentBase, IDisposable
{
    /// <summary>The trail this step belongs to.</summary>
    [CascadingParameter]
    private VetoBreadcrumbs? Parent { get; set; }

    /// <summary>The step's text. Ignored when <see cref="ChildContent"/> is given.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Where the step goes. Null renders it as text instead of a link.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>SVG markup drawn before the label, normally a constant from <c>VetoIcons</c>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>When true the step is drawn as text even if it carries an <see cref="Href"/>.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The step's content, for anything a plain label cannot say.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException(
                $"{nameof(VetoBreadcrumb)} must be placed inside a {nameof(VetoBreadcrumbs)}.");
        }

        Parent.Register(this);
    }

    /// <summary>
    /// The parameters as of the last time the trail was told about them. Compared rather than
    /// notified blindly: the trail re-renders its children, so an unconditional notification on
    /// every parameter set would re-render the trail, which would set them again, for ever.
    /// </summary>
    private (string? Label, string? Href, string? Icon, bool Disabled) _reported;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        var current = (Label, Href, Icon, Disabled);

        if (current == _reported)
        {
            return;
        }

        _reported = current;
        Parent?.NotifyChanged();
    }

    /// <summary>Nothing: the enclosing trail draws every step.</summary>
    protected override void BuildRenderTree ( Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder )
    {
    }

    /// <inheritdoc />
    public void Dispose () => Parent?.Unregister(this);
}
