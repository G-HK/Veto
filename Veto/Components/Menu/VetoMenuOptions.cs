namespace Veto.Components.Menu;

/// <summary>Which corner of the trigger the panel hangs from.</summary>
/// <remarks>
/// Placement is resolved in CSS against the trigger, not measured against the viewport, so a
/// menu near the edge of the window does not flip itself. Choose the placement that suits where
/// the trigger sits - a menu at the right of a toolbar wants <see cref="BottomEnd"/> - until
/// Veto has one shared popover positioning engine to do it automatically.
/// </remarks>
public enum VetoMenuPlacement
{
    /// <summary>Below the trigger, left edges aligned. The default.</summary>
    BottomStart,

    /// <summary>Below the trigger, right edges aligned. For a trigger near the right of its row.</summary>
    BottomEnd,

    /// <summary>Above the trigger, left edges aligned. For a trigger near the bottom of the page.</summary>
    TopStart,

    /// <summary>Above the trigger, right edges aligned.</summary>
    TopEnd
}

/// <summary>Turns the menu enums into the modifier suffixes their CSS uses.</summary>
public static class VetoMenuExtensions
{
    /// <summary>Returns the modifier suffix for a placement, e.g. <c>bottom-end</c>.</summary>
    public static string ToModifier ( this VetoMenuPlacement placement ) => placement switch
    {
        VetoMenuPlacement.BottomEnd => "bottom-end",
        VetoMenuPlacement.TopStart => "top-start",
        VetoMenuPlacement.TopEnd => "top-end",
        _ => "bottom-start"
    };

    /// <summary>Returns the modifier suffix for an appearance, e.g. <c>icon-only</c>.</summary>
    public static string ToModifier ( this VetoMenuAppearance appearance ) => appearance switch
    {
        VetoMenuAppearance.IconOnly => "icon-only",
        VetoMenuAppearance.TextOnly => "text-only",
        _ => "icon-and-text"
    };

    /// <summary>True when the panel hangs above the trigger rather than below it.</summary>
    internal static bool IsAbove ( this VetoMenuPlacement placement ) =>
        placement is VetoMenuPlacement.TopStart or VetoMenuPlacement.TopEnd;
}

/// <summary>What opens the menu.</summary>
public enum VetoMenuTrigger
{
    /// <summary>A click or Enter on the trigger. The default, and the one that works on touch.</summary>
    Click,

    /// <summary>
    /// The pointer resting on the trigger, after a short delay. A click still opens it and the
    /// keyboard still reaches it, so the menu never becomes hover-only.
    /// </summary>
    Hover
}

/// <summary>How much of an item is drawn.</summary>
public enum VetoMenuAppearance
{
    /// <summary>The icon and the text. The default.</summary>
    IconAndText,

    /// <summary>
    /// The icon alone. The text is still the item's accessible name and its tooltip, so nothing
    /// is lost to a screen reader - only to the eye.
    /// </summary>
    IconOnly,

    /// <summary>The text alone, with the space an icon would take left out.</summary>
    TextOnly
}

/// <summary>
/// The open menu, cascaded to the items inside it. Items use it to register themselves for
/// keyboard navigation, to read the appearance the menu was given, and to tell the menu they
/// were chosen so it can close.
/// </summary>
/// <remarks>
/// This exists so the items stay ordinary composable children - <c>VetoMenuItem</c> placed in
/// the menu's content, in any order, mixed with dividers and labels - rather than data passed
/// through a parameter. The menu still needs their order and their elements to move focus
/// between them, and registration is how it learns both without owning the markup.
/// </remarks>
public sealed class VetoMenuContext
{
    private readonly List<VetoMenuItem> _items = [];

    internal VetoMenuContext ( VetoMenu menu )
    {
        Menu = menu;
    }

    /// <summary>The items currently rendered, in document order.</summary>
    internal IReadOnlyList<VetoMenuItem> Items => _items;

    private VetoMenu Menu { get; }

    /// <summary>How much of each item is drawn.</summary>
    public VetoMenuAppearance Appearance => Menu.Appearance;

    /// <summary>Whether the items are drawn tightly.</summary>
    public bool Dense => Menu.Dense;

    /// <summary>Adds an item to the keyboard order. Called by the item as it initialises.</summary>
    internal void Register ( VetoMenuItem item )
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    /// <summary>Removes an item from the keyboard order.</summary>
    internal void Unregister ( VetoMenuItem item ) => _items.Remove(item);

    /// <summary>
    /// Tells the menu an item was chosen, so it can close and hand focus back to the trigger.
    /// </summary>
    internal Task ItemChosenAsync () => Menu.ItemChosenAsync();
}