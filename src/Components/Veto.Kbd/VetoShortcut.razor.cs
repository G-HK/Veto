using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Kbd;

/// <summary>
/// One row of a <see cref="VetoShortcutList"/>: what the shortcut does on the left, the keys
/// that do it on the right.
/// </summary>
/// <remarks>
/// The two are pushed to opposite edges so a column of them lines its keys up on the right,
/// which is what makes a reference list scannable - the eye runs down one column of keys
/// rather than hunting for them at the end of sentences of different lengths.
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoShortcutList&gt;
///     &lt;VetoShortcut Description="Open the command palette" Keys="Ctrl+K" /&gt;
///     &lt;VetoShortcut Description="Save" Keys="Ctrl+S" /&gt;
/// &lt;/VetoShortcutList&gt;
/// </code>
/// </example>
public partial class VetoShortcut : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-shortcut";

    /// <summary>What the shortcut does. Ignored when <see cref="ChildContent"/> is given.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The description, when it needs markup rather than plain text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The keys, as one string - <c>Ctrl+Shift+P</c>.</summary>
    [Parameter]
    public string? Keys { get; set; }

    /// <summary>The keys, when the row needs something other than a plain combination.</summary>
    [Parameter]
    public RenderFragment? KeysContent { get; set; }

    /// <summary>What <see cref="Keys"/> is split on.</summary>
    [Parameter]
    public string Separator { get; set; } = "+";

    /// <summary>Whether the modifiers are drawn as their conventional glyphs.</summary>
    [Parameter]
    public bool Symbols { get; set; }

    /// <summary>When false the keys sit side by side with no character between them.</summary>
    [Parameter]
    public bool ShowSeparator { get; set; } = true;

    /// <summary>Whether <c>Mod</c> is drawn as Cmd (true) or Ctrl (false).</summary>
    [Parameter]
    public bool Mac { get; set; }

    /// <summary>When false the key caps lose their border and raised edge.</summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>The size of the key caps in this row.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Small;
}

/// <summary>
/// A reference list of keyboard shortcuts - descriptions on the left, keys aligned down the
/// right.
/// </summary>
/// <remarks>
/// Rendered as a description list, because that is what it is: each row is a term and its
/// definition. The left/right layout is presentation on top of that, not a replacement for it.
/// </remarks>
public partial class VetoShortcutList : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-shortcut-list";

    /// <summary>The <see cref="VetoShortcut"/> rows.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>When true a hairline runs between the rows.</summary>
    [Parameter]
    public bool Divided { get; set; }

    /// <summary>How much room each row takes.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("divided"), Divided);
}
