using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Navigation;

/// <summary>
/// A trail of steps showing where you are, with the last step marked as the current one.
/// </summary>
/// <remarks>
/// <para>
/// Steps come either from <see cref="Items"/> or from <see cref="VetoBreadcrumb"/> children -
/// the list for a trail built from data, the children when a step needs markup of its own. Both
/// may be used together; the children follow the list.
/// </para>
/// <para>
/// A step is only a link when it has an <c>Href</c>, so the same component serves a trail that
/// is not navigation at all: a file path, the steps of a wizard, a position inside a document.
/// </para>
/// <para>
/// A long trail collapses: set <see cref="MaxItems"/> and the middle is replaced by a button
/// that expands it, rather than wrapping onto a second line.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoBreadcrumbs Items="_trail" /&gt;
///
/// &lt;VetoBreadcrumbs&gt;
///     &lt;VetoBreadcrumb Href="/" Icon="@VetoIcons.Home"&gt;Home&lt;/VetoBreadcrumb&gt;
///     &lt;VetoBreadcrumb&gt;Current page&lt;/VetoBreadcrumb&gt;
/// &lt;/VetoBreadcrumbs&gt;
/// </code>
/// </example>
public partial class VetoBreadcrumbs : VetoComponentBase
{
    private readonly List<VetoBreadcrumb> _children = [];

    private bool _expanded;

    private bool _childrenChanged;

    /// <inheritdoc />
    protected override string RootClass => "veto-breadcrumbs";

    /// <summary>The steps, in order from the root of the trail to where you are now.</summary>
    [Parameter]
    public IReadOnlyList<VetoBreadcrumbItem>? Items { get; set; }

    /// <summary><see cref="VetoBreadcrumb"/> children, for steps that need markup of their own.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>What is drawn between two steps when no icon or fragment is given.</summary>
    [Parameter]
    public string Separator { get; set; } = "/";

    /// <summary>SVG markup drawn between two steps, normally a constant from <c>VetoIcons</c>. Beats <see cref="Separator"/>.</summary>
    [Parameter]
    public string? SeparatorIcon { get; set; }

    /// <summary>Arbitrary markup drawn between two steps. Beats both <see cref="SeparatorIcon"/> and <see cref="Separator"/>.</summary>
    [Parameter]
    public RenderFragment? SeparatorContent { get; set; }

    /// <summary>
    /// The most steps to show before the middle collapses into a single expand button. Zero -
    /// the default - never collapses.
    /// </summary>
    [Parameter]
    public int MaxItems { get; set; }

    /// <summary>How many steps stay visible at the start of a collapsed trail.</summary>
    [Parameter]
    public int ItemsBeforeCollapse { get; set; } = 1;

    /// <summary>How many steps stay visible at the end of a collapsed trail.</summary>
    [Parameter]
    public int ItemsAfterCollapse { get; set; } = 1;

    /// <summary>What the expand button shows in place of the hidden steps.</summary>
    [Parameter]
    public string CollapsedSeparator { get; set; } = "…";

    /// <summary>Accessible name for the expand button.</summary>
    [Parameter]
    public string ExpandLabel { get; set; } = "Show the rest of the trail";

    /// <summary>
    /// The trail's accessible name. Leave it as "Breadcrumb" for navigation; rename it when the
    /// trail is something else, so it is not announced as navigation it is not.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Breadcrumb";

    /// <summary>
    /// When true the last step is a link like any other. Off by default: the last step is where
    /// you already are, and a link to the current page is noise.
    /// </summary>
    [Parameter]
    public bool LinkCurrent { get; set; }

    /// <summary>The trail's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>Raised when a step is activated, with the step's item.</summary>
    [Parameter]
    public EventCallback<VetoBreadcrumbItem> OnItemClick { get; set; }

    /// <summary>One laid-out step: an item, a child's content, or the collapsed placeholder.</summary>
    private sealed record Step (
        string? Label,
        string? Href,
        string? Icon,
        bool Disabled,
        RenderFragment? Content,
        bool Collapsed,
        VetoBreadcrumbItem? Source );

    /// <summary>The steps as they are drawn - collapsed, if the trail is long and <see cref="MaxItems"/> says so.</summary>
    private IReadOnlyList<Step> Steps => Collapse(AllSteps());

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("collapsed"), !_expanded && MaxItems > 0);

    /// <inheritdoc />
    protected override void OnAfterRender ( bool firstRender )
    {
        // Children register while this component is rendering, so their first appearance needs a
        // second pass. Flagged rather than re-rendered on the spot, which would loop.
        if (_childrenChanged)
        {
            _childrenChanged = false;
            StateHasChanged();
        }
    }

    /// <summary>Adds a child step. Called by <see cref="VetoBreadcrumb"/>; not part of the public API.</summary>
    internal void Register ( VetoBreadcrumb child )
    {
        _children.Add(child);
        _childrenChanged = true;
    }

    /// <summary>Removes a child step.</summary>
    internal void Unregister ( VetoBreadcrumb child )
    {
        if (_children.Remove(child))
        {
            _childrenChanged = true;
        }
    }

    /// <summary>Tells the trail one of its children changed, so the layout is rebuilt.</summary>
    internal void NotifyChanged () => _childrenChanged = true;

    private List<Step> AllSteps ()
    {
        var steps = new List<Step>();

        if (Items is not null)
        {
            steps.AddRange(Items.Select(item =>
                new Step(item.Label, item.Href, item.Icon, item.Disabled, null, false, item)));
        }

        steps.AddRange(_children.Select(child => new Step(
            child.Label,
            child.Href,
            child.Icon,
            child.Disabled,
            child.ChildContent,
            false,
            new VetoBreadcrumbItem(child.Label ?? string.Empty, child.Href, child.Icon, child.Disabled))));

        return steps;
    }

    private IReadOnlyList<Step> Collapse ( List<Step> steps )
    {
        if (_expanded || MaxItems <= 0 || steps.Count <= MaxItems)
        {
            return steps;
        }

        var before = Math.Max(0, ItemsBeforeCollapse);
        var after = Math.Max(0, ItemsAfterCollapse);

        // Collapsing has to remove more than it adds, or the trail gets longer instead of shorter.
        if (before + after + 1 >= steps.Count)
        {
            return steps;
        }

        return
        [
            .. steps.Take(before),
            new Step(null, null, null, false, null, true, null),
            .. steps.Skip(steps.Count - after)
        ];
    }

    private bool IsLink ( Step step, bool last )
        => step.Href is not null && !step.Disabled && (!last || LinkCurrent);

    /// <summary>
    /// True when a step that is not a link should still be activatable. A trail that drives
    /// state rather than the router has no hrefs to hang a click on, so the step becomes a
    /// button - a real one, so the keyboard reaches it like anything else.
    /// </summary>
    private bool IsAction ( Step step, bool last )
        => OnItemClick.HasDelegate
           && !IsLink(step, last)
           && !step.Disabled
           && step.Source is not null
           && (!last || LinkCurrent);

    private void Expand () => _expanded = true;

    private Task OnStepClickAsync ( Step step )
        => OnItemClick.HasDelegate && step.Source is not null
            ? OnItemClick.InvokeAsync(step.Source)
            : Task.CompletedTask;
}
