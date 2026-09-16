namespace Veto.Components.Handbook;

/// <summary>
/// Shared state for one handbook page and its table of contents.
/// </summary>
/// <remarks>
/// The two are separate components so either can be used on its own, which leaves them with no
/// way to talk to each other. Passing both the same state is what lets choosing a section in the
/// contents rail highlight it in the article. Without it each still works; they just act
/// independently.
/// </remarks>
public sealed class VetoHandbookState
{
    /// <summary>Raised when a section is chosen.</summary>
    public event Action<string>? SectionSelected;

    /// <summary>The section most recently chosen, or null when none has been.</summary>
    public string? Selected { get; private set; }

    /// <summary>Records a chosen section and notifies anything listening.</summary>
    public void Select ( string sectionId )
    {
        Selected = sectionId;
        SectionSelected?.Invoke(sectionId);
    }
}
