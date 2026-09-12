using Microsoft.AspNetCore.Components;

namespace Veto.Components.Handbook;

/// <summary>
/// One section of a <see cref="VetoHandbookPage"/>: a heading, an anchor, and its body.
/// </summary>
/// <remarks>
/// The same list feeds the page and its table of contents, so the two can never disagree about
/// what is on the page or what the anchors are called.
/// </remarks>
public sealed record VetoHandbookSection
{
    /// <summary>
    /// The anchor for this section, used as the element's <c>id</c> and as the link target.
    /// Keep it URL-safe: lowercase, hyphens, no spaces.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>The section's heading.</summary>
    public required string Title { get; init; }

    /// <summary>The section's body as plain text. Ignored when <see cref="Content"/> is set.</summary>
    public string? Body { get; init; }

    /// <summary>The section's body as markup - code blocks, lists, components.</summary>
    public RenderFragment? Content { get; init; }
}
