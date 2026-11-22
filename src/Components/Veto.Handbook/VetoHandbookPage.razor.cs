using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Handbook;

/// <summary>
/// A documentation article: an eyebrow, a title, a lead paragraph, and a list of anchored
/// sections. Pairs with <see cref="VetoHandbookToc"/>, which builds its table of contents from
/// the same list.
/// </summary>
/// <remarks>
/// Usable on its own, inside any layout - it renders an <c>&lt;article&gt;</c> and nothing else.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoDocsLayout Items="Nav"&gt;
///     &lt;Toc&gt;&lt;VetoHandbookToc Sections="Sections" /&gt;&lt;/Toc&gt;
///     &lt;ChildContent&gt;
///         &lt;VetoHandbookPage Eyebrow="Guides"
///                           Title="Getting started"
///                           Lead="Install the package and render your first component."
///                           Sections="Sections" /&gt;
///     &lt;/ChildContent&gt;
/// &lt;/VetoDocsLayout&gt;
/// </code>
/// </example>
public partial class VetoHandbookPage : VetoComponentBase, IDisposable
{
    private CancellationTokenSource? _fadeHighlight;
    private string? _highlighted;
    // The instance actually subscribed to, which is not necessarily the current State: when
    // the parameter changes, the old one has to be let go and the new one taken up.
    private VetoHandbookState? _subscribed;

    /// <inheritdoc />
    protected override string RootClass => "veto-handbook-page";

    /// <summary>The page's sections, in the order they should appear.</summary>
    [Parameter]
    public IReadOnlyList<VetoHandbookSection> Sections { get; set; } = [];

    /// <summary>The page's heading.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>A short label above the title - the part of the documentation this page belongs to.</summary>
    [Parameter]
    public string? Eyebrow { get; set; }

    /// <summary>One sentence saying what the page covers.</summary>
    [Parameter]
    public string? Lead { get; set; }

    /// <summary>Extra content in the page header, below the lead - badges, metadata, actions.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>Content rendered after the last section.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// State shared with this page's <see cref="VetoHandbookToc"/>. Supply the same instance to
    /// both for choosing a section in the rail to highlight it here.
    /// </summary>
    [Parameter]
    public VetoHandbookState? State { get; set; }

    /// <summary>
    /// Briefly highlights a section when the reader jumps to it, so it is obvious where they
    /// landed on a long page. Requires <see cref="State"/> to be shared with the contents rail.
    /// </summary>
    [Parameter]
    public bool HighlightOnNavigate { get; set; } = true;

    /// <summary>
    /// How long the highlight lasts, in milliseconds. Ignored when
    /// <see cref="HighlightPersists"/> is set.
    /// </summary>
    [Parameter]
    public int HighlightDuration { get; set; } = 5000;

    /// <summary>
    /// Keeps the highlight until another section is chosen, rather than fading it after
    /// <see cref="HighlightDuration"/>. Off by default.
    /// </summary>
    [Parameter]
    public bool HighlightPersists { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (!ReferenceEquals(State, _subscribed))
        {
            if (_subscribed is not null)
            {
                _subscribed.SectionSelected -= OnSectionSelected;
            }

            if (State is not null)
            {
                State.SectionSelected += OnSectionSelected;
            }

            _subscribed = State;
        }
    }

    private string? HighlightClass ( VetoHandbookSection section )
        => _highlighted == section.Id ? "veto-handbook-page__section--highlighted" : null;

    private async void OnSectionSelected ( string sectionId )
    {
        if (!HighlightOnNavigate)
        {
            return;
        }

        _highlighted = sectionId;
        await InvokeAsync(StateHasChanged);

        if (HighlightPersists)
        {
            return;
        }

        // A newer selection cancels the previous fade, so jumping twice in quick succession
        // does not clear the highlight that has only just been applied.
        _fadeHighlight?.Cancel();
        _fadeHighlight?.Dispose();
        _fadeHighlight = new CancellationTokenSource();

        try
        {
            await Task.Delay(HighlightDuration, _fadeHighlight.Token);

            _highlighted = null;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a later selection, which owns the highlight now.
        }
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        if (_subscribed is not null)
        {
            _subscribed.SectionSelected -= OnSectionSelected;
        }

        _fadeHighlight?.Cancel();
        _fadeHighlight?.Dispose();
    }

    /// <summary>The path a section's anchor points at.</summary>
    /// <remarks>
    /// Built as a full path plus fragment rather than a bare <c>#id</c>. An app with
    /// <c>&lt;base href="/"&gt;</c> resolves a fragment-only link against that base rather than
    /// against the current URL, so <c>#install</c> would navigate to the home page.
    /// </remarks>
    private string AnchorHref ( VetoHandbookSection section )
        => VetoHandbookNavigation.AnchorHref(Nav, section.Id);

    private string? SectionTestId ( VetoHandbookSection section )
        => TestId is null ? null : $"{TestId}-{section.Id}";
}
