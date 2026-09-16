using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Kbd;

/// <summary>
/// A keyboard key, or a combination of them, drawn as physical key caps.
/// </summary>
/// <remarks>
/// <para>
/// For documentation, tooltips and menu hints - anywhere the reader has to tell a key they
/// press from the prose around it.
/// </para>
/// <para>
/// A combination is written as one string: <c>Keys="Ctrl+Shift+P"</c>. It is split into a
/// <c>&lt;kbd&gt;</c> per key nested inside an outer one, which is what the HTML spec actually
/// prescribes for a chord, and what gives each key its own cap instead of one long pill.
/// </para>
/// <para>
/// Key names are printed as written by default. <see cref="Symbols"/> swaps the ones that have
/// a conventional glyph - <c>Shift</c> for ⇧, <c>Cmd</c> for ⌘ - which looks right on a Mac but
/// reads poorly aloud, so it is off unless asked for.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Press &lt;VetoKbd Keys="Ctrl+K" /&gt; to open the palette.
/// &lt;VetoKbd Keys="Cmd+Shift+P" Symbols="true" Size="VetoSize.Small" /&gt;
/// </code>
/// </example>
public partial class VetoKbd : VetoComponentBase
{
    private static readonly Dictionary<string, string> Glyphs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cmd"] = "⌘",
        ["command"] = "⌘",
        ["meta"] = "⌘",
        ["shift"] = "⇧",
        ["alt"] = "⌥",
        ["option"] = "⌥",
        ["ctrl"] = "⌃",
        ["control"] = "⌃",
        ["enter"] = "↵",
        ["return"] = "↵",
        ["backspace"] = "⌫",
        ["delete"] = "⌦",
        ["tab"] = "⇥",
        ["escape"] = "esc",
        ["up"] = "↑",
        ["arrowup"] = "↑",
        ["down"] = "↓",
        ["arrowdown"] = "↓",
        ["left"] = "←",
        ["arrowleft"] = "←",
        ["right"] = "→",
        ["arrowright"] = "→",
        ["space"] = "space",
    };

    /// <inheritdoc />
    protected override string RootClass => "veto-kbd";

    /// <summary>
    /// The key, or the combination, as one string - <c>Ctrl+Shift+P</c>. Split on
    /// <see cref="Separator"/>. Leave it null and use <see cref="ChildContent"/> for a single
    /// key whose text needs markup.
    /// </summary>
    [Parameter]
    public string? Keys { get; set; }

    /// <summary>A single key's content, when <see cref="Keys"/> is not used.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>What <see cref="Keys"/> is split on, and what is drawn between the caps.</summary>
    [Parameter]
    public string Separator { get; set; } = "+";

    /// <summary>When false the keys sit side by side with no character between them.</summary>
    [Parameter]
    public bool ShowSeparator { get; set; } = true;

    /// <summary>
    /// When true the modifiers with a conventional glyph are drawn as that glyph. Off by
    /// default: a screen reader makes a poor job of reading ⌘ aloud.
    /// </summary>
    [Parameter]
    public bool Symbols { get; set; }

    /// <summary>The size of the key caps. Small is the one for running inline inside body text.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>
    /// When false the cap loses its border and its raised edge, leaving the key as tinted text.
    /// For dense hints - a menu, a tooltip - where a row of little 3D caps is more furniture
    /// than the hint is worth.
    /// </summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>The individual keys, in order. Empty when <see cref="Keys"/> was not given.</summary>
    private IReadOnlyList<string> Parts => string.IsNullOrWhiteSpace(Keys)
        ? []
        : [.. Keys.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("borderless"), !Bordered);

    private string Display ( string key )
        => Symbols && Glyphs.TryGetValue(key, out var glyph) ? glyph : key;
}
