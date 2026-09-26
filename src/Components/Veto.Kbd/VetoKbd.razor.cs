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
/// Key names are printed as written by default, except <c>Mod</c>, which is <c>Ctrl</c> or
/// <c>Cmd</c> depending on <see cref="Mac"/>. <see cref="Symbols"/> swaps the ones that have
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

    /// <summary>
    /// Whether the keys are for a Mac. Decides what the platform-neutral <c>Mod</c> key is
    /// drawn as: <c>Cmd</c> on a Mac, <c>Ctrl</c> everywhere else - so one string,
    /// <c>Mod+S</c>, documents both.
    /// </summary>
    [Parameter]
    public bool Mac { get; set; }

    /// <summary>The individual keys, in order. Empty when <see cref="Keys"/> was not given.</summary>
    private IReadOnlyList<string> Parts => Split(Keys, Separator);

    /// <summary>
    /// Splits a combination into its keys. The separator doubles as a key of its own, so
    /// <c>Ctrl++</c> is Ctrl and Plus rather than Ctrl alone: an empty entry followed by
    /// another empty entry is the separator key.
    /// </summary>
    private static IReadOnlyList<string> Split ( string? keys, string separator )
    {
        if (string.IsNullOrWhiteSpace(keys))
        {
            return [];
        }

        if (string.IsNullOrEmpty(separator))
        {
            return [keys.Trim()];
        }

        var tokens = keys.Split(separator, StringSplitOptions.TrimEntries);
        var parts = new List<string>(tokens.Length);

        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Length > 0)
            {
                parts.Add(tokens[i]);
            }
            else if (i + 1 < tokens.Length && tokens[i + 1].Length == 0)
            {
                parts.Add(separator);
                i++;
            }
        }

        return parts;
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("borderless"), !Bordered);

    private string Resolve ( string key )
        => key.Equals("mod", StringComparison.OrdinalIgnoreCase) ? (Mac ? "Cmd" : "Ctrl") : key;

    private string Display ( string key )
    {
        key = Resolve(key);
        return Symbols && Glyphs.TryGetValue(key, out var glyph) ? glyph : key;
    }

    // A glyph is a poor read on its own; the key's name goes in a tooltip.
    private string? Title ( string key )
    {
        key = Resolve(key);
        return Symbols && Glyphs.ContainsKey(key) ? key : null;
    }
}
