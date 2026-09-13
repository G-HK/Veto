using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Menu;

/// <summary>
/// A heading over a group of items in a <see cref="VetoMenu"/> - "Export as", "Sort by".
/// </summary>
/// <remarks>
/// Marked <c>role="presentation"</c>: a menu's children are expected to be menu items, and an
/// unannounced heading among them confuses the count a screen reader reports. The grouping is
/// visual; where it has to be announced as well, give the menu itself a label that says it.
/// </remarks>
public partial class VetoMenuLabel : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-menu-label";

    /// <summary>The heading's text.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>The heading's content, for when <see cref="Text"/> is not enough.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}