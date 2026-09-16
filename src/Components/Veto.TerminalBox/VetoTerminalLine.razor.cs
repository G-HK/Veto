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

    /// <summary>
    /// The prompt drawn before the command. Any sign you like - <c>$</c>, <c>&gt;</c>,
    /// <c>PS&gt;</c>, <c>~/veto $</c>. Null renders none.
    /// </summary>
    /// <remarks>
    /// Two-way bindable with <c>@bind-Prompt</c>, which is only worth doing alongside
    /// <see cref="PromptEditable"/> - nothing else in the line changes it.
    /// </remarks>
    [Parameter]
    public string? Prompt { get; set; } = "$";

    /// <summary>Raised when the prompt is edited, so <c>@bind-Prompt</c> works.</summary>
    [Parameter]
    public EventCallback<string?> PromptChanged { get; set; }

    /// <summary>
    /// Content drawn in place of <see cref="Prompt"/> - an icon, a coloured host name, a
    /// branch. For a prompt being used to say something rather than to mark a line.
    /// </summary>
    /// <remarks>
    /// Unlike a bare sign, this is not hidden from assistive technology: a <c>$</c> before
    /// every line is noise, but content put here deliberately is presumed worth reading.
    /// </remarks>
    [Parameter]
    public RenderFragment? PromptContent { get; set; }

    /// <summary>
    /// Whether the reader can type over the prompt. Off by default. Turning it on is what
    /// makes <c>@bind-Prompt</c> mean anything, and what lets the prompt be used as a small
    /// input of its own - a host, a profile, a working directory.
    /// </summary>
    [Parameter]
    public bool PromptEditable { get; set; }

    /// <summary>
    /// The editable prompt's accessible name. It has no visible label, so without this a
    /// screen reader announces an unnamed text box.
    /// </summary>
    [Parameter]
    public string PromptLabel { get; set; } = "Prompt";

    /// <summary>
    /// The editable prompt's width in characters, so the field is the size of what is in it
    /// rather than the browser's default twenty.
    /// </summary>
    private int PromptSize => Math.Max(1, Prompt?.Length ?? 1);

    private Task OnPromptChangedAsync ( ChangeEventArgs args )
    {
        var value = args.Value?.ToString();

        Prompt = value;

        return PromptChanged.HasDelegate
            ? PromptChanged.InvokeAsync(value)
            : Task.CompletedTask;
    }

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
            .AddClass(Modifier("prompt-editable"), PromptEditable && PromptContent is null)
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