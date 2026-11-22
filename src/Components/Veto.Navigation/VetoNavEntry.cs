namespace Veto.Components.Navigation;

/// <summary>
/// One entry in a <see cref="VetoNavMenu"/> - a link, or a group with its own children.
/// </summary>
/// <remarks>
/// A declarative alternative to nesting markup: an app can build its navigation as data,
/// which is easier to share between a drawer, a dock and a footer without repeating it.
/// </remarks>
public sealed record VetoNavEntry
{
    /// <summary>The text shown for the entry.</summary>
    public required string Label { get; init; }

    /// <summary>Where the entry navigates. Null for a group heading.</summary>
    public string? Href { get; init; }

    /// <summary>SVG markup for the entry's icon, normally a constant from <c>VetoIcons</c>.</summary>
    public string? Icon { get; init; }

    /// <summary>Short trailing text - a count, a status, "new".</summary>
    public string? Badge { get; init; }

    /// <summary>Child entries. Any entry with children renders as a collapsible group.</summary>
    public IReadOnlyList<VetoNavEntry> Children { get; init; } = [];

    /// <summary>
    /// Whether the entry matches the current URL only exactly, rather than as a prefix. Set
    /// this on a root link such as "/", which would otherwise match every page.
    /// </summary>
    public bool MatchExact { get; init; }

    /// <summary>Whether the entry is shown but not activatable.</summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// For a group: whether it starts closed. The reader can still open it, and a group holding
    /// the current page is always open.
    /// </summary>
    public bool Collapsed { get; init; }

    /// <summary>True when this entry has children and therefore renders as a group.</summary>
    public bool IsGroup => Children.Count > 0;
}
