using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.TerminalBox;

/// <summary>
/// A terminal window: chrome, a title bar and a copy button around one or more
/// <see cref="VetoTerminalLine"/>s. For the "here is the command to run" block on a landing
/// page or an installation guide, where a plain code block would read as an afterthought.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Command"/> for the single-line case and child lines for anything longer -
/// a command followed by its output, or a short session. Both can be combined; the
/// <see cref="Command"/> line renders first.
/// </para>
/// <para>
/// This is presentation, not an emulator: nothing is executed, and the typing animation is a
/// CSS effect over text that is already in the DOM. A screen reader is given the whole command
/// at once rather than a character at a time.
/// </para>
/// </remarks>
public partial class VetoTerminalBox : VetoComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private CancellationTokenSource? _resetCopied;
    private bool _copied;

    /// <inheritdoc />
    protected override string RootClass => "veto-terminal-box";

    /// <summary>The title shown in the bar. Null renders the bar without a title.</summary>
    [Parameter]
    public string? Title { get; set; } = "Terminal";

    /// <summary>
    /// The icon beside the title, normally a constant from <c>VetoIcons</c>. Null renders none.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; } = VetoIcons.Terminal;

    /// <summary>
    /// Whether the title bar is rendered at all. False leaves the body on its own - a bare
    /// terminal, for a page that has already said what it is.
    /// </summary>
    [Parameter]
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// A single command, as a shorthand for one child <see cref="VetoTerminalLine"/>. The
    /// window's most common shape is one line long, and this saves nesting for it.
    /// </summary>
    [Parameter]
    public string? Command { get; set; }

    /// <summary>The prompt drawn before <see cref="Command"/>.</summary>
    [Parameter]
    public string? Prompt { get; set; } = "$";

    /// <summary>Whether <see cref="Command"/> types itself in rather than appearing at once.</summary>
    [Parameter]
    public bool Typed { get; set; }

    /// <summary>Whether a typed <see cref="Command"/> repeats, or types once and stays.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>How long one cycle of the typing animation takes, as a CSS duration.</summary>
    [Parameter]
    public string TypingDuration { get; set; } = "8s";

    /// <summary>The window's lines - normally <see cref="VetoTerminalLine"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether to offer a copy button in the title bar. True by default.</summary>
    [Parameter]
    public bool Copyable { get; set; } = true;

    /// <summary>
    /// What the copy button puts on the clipboard. Null - the default - copies the commands in
    /// the window, skipping prompts and output lines, which is what someone pressing copy on a
    /// terminal wants to paste into their own.
    /// </summary>
    [Parameter]
    public string? CopyText { get; set; }

    /// <summary>The copy button's accessible name and tooltip, before it has been used.</summary>
    [Parameter]
    public string CopyLabel { get; set; } = "Copy command";

    /// <summary>The copy button's accessible name and tooltip for the moment after a successful copy.</summary>
    [Parameter]
    public string CopiedLabel { get; set; } = "Copied";

    /// <summary>
    /// Raised after the copy button is used, with whether the browser accepted the write. It
    /// refuses outside a secure context, which is worth surfacing rather than swallowing.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnCopied { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("headless"), !ShowHeader);

    private async Task CopyAsync ()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Veto.TerminalBox/veto-terminal-box.js");

            // Collected from the DOM rather than from the child components. A line that
            // registered itself with this one would have to compare its parameters before
            // notifying, or the two would re-render each other without end - and the rendered
            // text is the honest answer anyway, whatever produced it.
            var text = CopyText ?? await _module.InvokeAsync<string>("collectCommands", RootElement);

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _copied = await _module.InvokeAsync<bool>("copyText", text);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            _copied = false;
        }

        if (OnCopied.HasDelegate)
        {
            await OnCopied.InvokeAsync(_copied);
        }

        if (_copied)
        {
            // Rendered here rather than left to the render that follows the event. This method
            // awaits the reset below, so the event's task does not complete for two seconds -
            // by which time the flag is false again and the confirmation would never have been
            // drawn at all.
            StateHasChanged();

            await ResetCopiedLabelAsync();
        }
    }

    /// <summary>
    /// Returns the button to its resting state after a moment, so the confirmation reads as
    /// feedback rather than as the button's permanent label.
    /// </summary>
    private async Task ResetCopiedLabelAsync ()
    {
        _resetCopied?.Cancel();
        _resetCopied?.Dispose();
        _resetCopied = new CancellationTokenSource();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), _resetCopied.Token);
            _copied = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Copied again before the delay elapsed; the newer reset takes over.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        _resetCopied?.Cancel();
        _resetCopied?.Dispose();

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The page is already gone.
            }
        }
    }
}