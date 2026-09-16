using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// A line between two buttons in a <see cref="VetoButtonGroup"/>, placed by hand.
/// </summary>
/// <remarks>
/// The group's <c>Separators</c> parameter draws a line between <em>every</em> child; this is
/// for the other case - a line between only some of them, which is how a toolbar clusters two
/// or three related actions and then breaks before the next cluster. It takes the group's
/// orientation, so it is a vertical hairline in a row and a horizontal one in a column.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoButtonGroup&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.Bold" Label="Bold" /&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.Italic" Label="Italic" /&gt;
///     &lt;VetoButtonGroupSeparator /&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.Link" Label="Link" /&gt;
/// &lt;/VetoButtonGroup&gt;
/// </code>
/// </example>
public partial class VetoButtonGroupSeparator : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-button-group__separator";
}