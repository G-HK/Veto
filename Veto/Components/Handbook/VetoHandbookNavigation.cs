using Microsoft.AspNetCore.Components;

namespace Veto.Components.Handbook;

/// <summary>
/// Builds in-page anchor links that survive a <c>&lt;base href&gt;</c>.
/// </summary>
/// <remarks>
/// A bare <c>#section</c> link resolves against the document's base URL, not the address bar.
/// In a Blazor app - which nearly always declares <c>&lt;base href="/"&gt;</c> - that sends the
/// reader to the home page instead of down the current one. Pairing the fragment with the
/// current path is what makes the link do what it looks like it does.
/// </remarks>
internal static class VetoHandbookNavigation
{
    /// <summary>Returns an absolute path plus fragment for the given anchor on the current page.</summary>
    public static string AnchorHref ( NavigationManager navigation, string anchorId )
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri);

        // Drop any query or fragment already on the address, so anchors never stack up.
        var path = relative.Split('?')[0].Split('#')[0];

        return $"/{path.TrimStart('/')}#{anchorId}";
    }
}