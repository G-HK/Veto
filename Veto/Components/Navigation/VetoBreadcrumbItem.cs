namespace Veto.Components.Navigation;

/// <summary>
/// One step in a breadcrumb trail.
/// </summary>
/// <remarks>
/// A step with no <see cref="Href"/> is rendered as plain text rather than a link, which is
/// what makes the trail useful for things that are not navigation at all - a file path, the
/// steps of a wizard, where you are inside a document.
/// </remarks>
/// <param name="Label">The step's text.</param>
/// <param name="Href">Where the step goes. Null renders it as text instead of a link.</param>
/// <param name="Icon">
/// Optional SVG markup drawn before the label, normally a constant from <c>VetoIcons</c> -
/// a house on the first step, a folder on a path.
/// </param>
/// <param name="Disabled">When true the step is drawn as text even if it carries an <see cref="Href"/>.</param>
public sealed record VetoBreadcrumbItem (
    string Label,
    string? Href = null,
    string? Icon = null,
    bool Disabled = false );
