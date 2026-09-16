using Microsoft.AspNetCore.Components;
using System.Globalization;
using Veto.Core;

namespace Veto.Components.TerminalBox;

/// <summary>
/// One line inside a <see cref="VetoTerminalBox"/>: a prompt, a command, and optionally the
/// output it produced.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Command"/> is what the window's copy button collects; <see cref="Output"/> is
/// not, because nobody wants to paste a program's own chatter back into their shell. A line
/// with only an <see cref="Output"/> is therefore skipped by copy entirely.
/// </para>
/// <para>
/// The typing animation is a width animation over text that is already present, stepped one
/// character at a time. Nothing is inserted or removed as it plays, so assistive technology
/// reads the whole command rather than watching it arrive, and the text stays selectable.
/// </para>
/// </remarks>
public partial class VetoTerminalLine : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-terminal-line";

    /// <summary>The prompt drawn before the command, e.g. <c>$</c>. Null renders none.</summary>
    [Parameter]
    public string? Prompt { get; set; } = "$";

    /// <summary>The command. This is the part the window's copy button collects.</summary>
    [Parameter]
    public string? Command { get; set; }

    /// <summary>
    /// Output the command produced, drawn muted and never copied. A line with output and no
    /// command is one of the program's own, so give it no <see cref="Prompt"/>.
    /// </summary>
    [Parameter]
    public string? Output { get; set; }

    /// <summary>Whether the command types itself in rather than appearing at once.</summary>
    [Parameter]
    public bool Typed { get; set; }

    /// <summary>
    /// Whether a typed command repeats, or types once and stays. Looping suits a landing page;
    /// a page the reader is working through wants it to settle.
    /// </summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>How long one cycle of the typing animation takes, as a CSS duration.</summary>
    [Parameter]
    public string TypingDuration { get; set; } = "8s";

    /// <summary>The line's content, when the prompt/command/output shape does not fit.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("typed"), Typed && Command is { Length: > 0 })
            .AddClass(Modifier("once"), Typed && !Loop);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            // The animation steps once per character and the width is measured in ch, so the
            // caret lands exactly on each character boundary of the monospaced text.
            .AddStyle(
                "--veto-terminal-chars",
                (Command?.Length ?? 0).ToString(CultureInfo.InvariantCulture),
                Command is { Length: > 0 })
            .AddStyle("--veto-terminal-typing-duration", TypingDuration, Typed);
}