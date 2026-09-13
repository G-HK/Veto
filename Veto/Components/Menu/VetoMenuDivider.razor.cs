using Veto.Core;

namespace Veto.Components.Menu;

/// <summary>
/// A line between groups of items in a <see cref="VetoMenu"/> - what separates the ordinary
/// actions from the destructive one at the bottom.
/// </summary>
/// <remarks>
/// Placed by hand rather than switched on for the whole menu, because where a menu breaks is
/// the point: a line after every item says nothing about which actions belong together.
/// </remarks>
public partial class VetoMenuDivider : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-menu-divider";
}