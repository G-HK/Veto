using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Components.Handbook;

/// <summary>
/// A table of contents for a <see cref="VetoHandbookPage"/>, built from the same section list so
/// the two cannot drift apart.
/// </summary>
public partial class VetoHandbookToc : VetoComponentBase, IAsyncDisposable
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
    private string? _spied;
    private IJSObjectReference? _module;
    private DotNetObjectReference<VetoHandbookToc>? _self;
    private string? _observedKey;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// When true the rail follows the reader: the section they have scrolled to is marked as
    /// current, not only the one they last clicked. Needs the sections' headings on the same
    /// page as the rail.
    /// </summary>
    [Parameter]
    public bool ScrollSpy { get; set; }

    /// <summary>
    /// How far below the top of the viewport, in pixels, a section's top has to pass before it
    /// counts as the one being read - the height of a fixed app bar, plus a little.
    /// </summary>
    [Parameter]
    public int ScrollSpyOffset { get; set; } = 96;

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
        var chosen = _selected ?? (ScrollSpy ? _spied : null);

        if (chosen is not null)
        {
            return string.Equals(chosen, section.Id, StringComparison.OrdinalIgnoreCase);
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

    // Re-renders so the current-section highlight follows the fragment as the reader moves. A
    // click's selection is dropped here: it has done its job once the address carries the
    // fragment, and kept any longer it outranked every later fragment - after Back, the rail
    // still marked the section clicked before it.
    private void OnLocationChanged ( object? sender, LocationChangedEventArgs args )
    {
        _selected = null;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by the scroll-spy script when the reader moves into another section.</summary>
    [JSInvokable]
    public Task OnSpiedSection ( string? sectionId )
    {
        _spied = sectionId;
        _selected = null;
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        // Observed again only when the sections themselves change - navigating to another
        // page, say - rather than on every render.
        var key = ScrollSpy && Visible && Sections.Count > 0
            ? $"{ScrollSpyOffset}|{string.Join('|', Sections.Select(s => s.Id))}"
            : null;

        if (key == _observedKey)
        {
            return;
        }

        // Recorded before the call, not after: observe() reports the current section at once,
        // and the render that report causes must see this key or it would observe again - and
        // be reported to again, for ever.
        _observedKey = key;

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Handbook/veto-handbook.js");

            if (key is null)
            {
                await _module.InvokeVoidAsync("unobserve", RootElement);
                _spied = null;
            }
            else
            {
                _self ??= DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("observe", RootElement, _self, Sections.Select(s => s.Id).ToArray(), ScrollSpyOffset);
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or the page has gone. The rail still works from clicks and fragments.
            _observedKey = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        Nav.LocationChanged -= OnLocationChanged;

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("unobserve", RootElement);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The page is already gone.
            }
        }

        _self?.Dispose();
    }
}
