using Microsoft.AspNetCore.Components;

namespace Veto.Components.Navigation;

/// <summary>
/// Works out whether a link points at the page currently being shown.
/// </summary>
/// <remarks>
/// Shared by <see cref="VetoNavMenu"/> and <see cref="VetoLink"/> so a link marks itself as the
/// current page by the same rule wherever it appears - a menu entry and a link in the body of a
/// page disagreeing about which one is "here" would be a strange thing to ship.
/// </remarks>
internal static class VetoRouteMatch
{
    /// <summary>The current URL's path, leading slash included and trailing slash removed.</summary>
    public static string CurrentPath ( NavigationManager navigation )
        => "/" + navigation.ToBaseRelativePath(navigation.Uri)
            .Split('?')[0]
            .Split('#')[0]
            .TrimEnd('/');

    /// <summary>
    /// Whether <paramref name="href"/> is the current page. A prefix match by default, so
    /// <c>/docs</c> stays lit while a reader is on <c>/docs/installation</c>; exact when asked,
    /// which the root link always needs because every path starts with "/".
    /// </summary>
    public static bool IsActive ( string currentPath, string? href, bool exact )
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return false;
        }

        // An absolute URL leaves the app, so it is never the page being shown.
        if (href.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        var path = "/" + href.TrimStart('/').TrimEnd('/');

        return exact || path == "/"
            ? string.Equals(currentPath, path, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(path, StringComparison.OrdinalIgnoreCase);
    }
}
