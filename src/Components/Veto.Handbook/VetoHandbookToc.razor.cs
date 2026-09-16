using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Veto.Core;

namespace Veto.Components.Handbook;

/// <summary>
/// A table of contents for a <see cref="VetoHandbookPage"/>, built from the same section list so
/// the two cannot drift apart.
/// </summary>
public partial class VetoHandbookToc : VetoComponentBase, IDisposable
{
    /// <inheritdoc />
    protected override string RootClass => "veto-handbook-toc";

    /// <summary>The sections to list, in page order.</summary>
    [Parameter]
    public IReadOnlyList<VetoHandbookSection> Sections { get; set; } = [];

    /// <summary>The heading above the list. Set to null to render none.</summary>
    [Parameter]
    public string? Title { get; set; } = "On this page";

    /// <summary>The navigation landmark's accessible name.</summary>
    [Parameter]
    public string Label { get; set; } = "On this page";

    /// <summary>
    /// State shared with the <see cref="VetoHandbookPage"/> this rail belongs to. Supply the same
    /// instance to both and choosing a section here can highlight it there.
    /// </summary>
    [Parameter]
    public VetoHandbookState? State { get; set; }

    private string? _selected;

    /// <inheritdoc />
    protected override void OnInitialized () => Nav.LocationChanged += OnLocationChanged;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        // Moving to another page clears a selection that no longer refers to anything here.
        if (_selected is not null && !Sections.Any(s => s.Id == _selected))
        {
            _selected = null;
        }
    }

    /// <summary>
    /// Whether a section is the one being read - either because it was just chosen here, or
    /// because the URL arrived with its fragment.
    /// </summary>
    /// <remarks>
    /// The click is tracked as well as the URL because a browser handles a same-page fragment
    /// link itself; Blazor does not reliably raise <c>LocationChanged</c> for it, so relying on
    /// the address alone leaves the highlight a step behind the reader.
    /// </remarks>
    private bool IsCurrent ( VetoHandbookSection section )
    {
        if (_selected is not null)
        {
            return string.Equals(_selected, section.Id, StringComparison.OrdinalIgnoreCase);
        }

        var fragment = Nav.Uri.Split('#');
        return fragment.Length > 1 && string.Equals(fragment[1], section.Id, StringComparison.OrdinalIgnoreCase);
    }

    private void Select ( VetoHandbookSection section )
    {
        _selected = section.Id;
        State?.Select(section.Id);
    }

    private string AnchorHref ( VetoHandbookSection section )
        => VetoHandbookNavigation.AnchorHref(Nav, section.Id);

    private string? LinkTestId ( VetoHandbookSection section )
        => TestId is null ? null : $"{TestId}-{section.Id}";

    // Re-renders so the current-section highlight follows the fragment as the reader moves.
    private void OnLocationChanged ( object? sender, LocationChangedEventArgs args )
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose () => Nav.LocationChanged -= OnLocationChanged;
}
