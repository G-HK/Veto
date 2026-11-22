using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Veto.Components.Icon;
using Veto.Components.Toast;
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

    /// <summary>
    /// Resolved through the provider rather than injected directly so the component still
    /// works in an app that never registered toasts. [Inject] on IVetoToastService would throw
    /// at first render there, which is a hard failure for an entirely optional convenience.
    /// </summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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

    /// <summary>
    /// The prompt drawn before <see cref="Command"/>. Any sign you like - <c>$</c>,
    /// <c>&gt;</c>, <c>PS&gt;</c>. Null renders none. Applies to the shorthand line only;
    /// child lines carry their own.
    /// </summary>
    /// <remarks>Two-way bindable with <c>@bind-Prompt</c> alongside <see cref="PromptEditable"/>.</remarks>
    [Parameter]
    public string? Prompt { get; set; } = "$";

    /// <summary>Raised when the shorthand line's prompt is edited, so <c>@bind-Prompt</c> works.</summary>
    [Parameter]
    public EventCallback<string?> PromptChanged { get; set; }

    /// <summary>
    /// Content drawn in place of <see cref="Prompt"/> on the shorthand line - an icon, a
    /// coloured host name, a branch.
    /// </summary>
    [Parameter]
    public RenderFragment? PromptContent { get; set; }

    /// <summary>Whether the reader can type over the shorthand line's prompt. Off by default.</summary>
    [Parameter]
    public bool PromptEditable { get; set; }

    /// <summary>The editable prompt's accessible name.</summary>
    [Parameter]
    public string PromptLabel { get; set; } = "Prompt";

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

    /// <summary>
    /// Whether a copy raises a toast. Needs a <c>VetoToastHost</c> in the layout and
    /// <c>AddVetoToast()</c> registered; without either, copying still works and says nothing.
    /// </summary>
    [Parameter]
    public bool NotifyOnCopy { get; set; } = true;

    /// <summary>What the toast says after a successful copy.</summary>
    [Parameter]
    public string CopyToastMessage { get; set; } = "Command copied to clipboard";

    /// <summary>
    /// What the toast says when the browser refused the write - outside a secure context, or
    /// with the clipboard permission denied.
    /// </summary>
    [Parameter]
    public string CopyFailedToastMessage { get; set; } = "Could not copy to the clipboard";

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

    /// <summary>
    /// Caps the height of the window's body, which then scrolls - for a long session or a log.
    /// Any CSS length. Null lets the window grow with its lines.
    /// </summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-terminal-max-height", MaxHeight, MaxHeight is not null);

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

        NotifyCopied();

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
    /// Raises the toast that reports the copy. The icon swap on the button is easy to miss -
    /// it is small, and the reader's eye is usually on what they just copied - so the toast is
    /// what actually confirms it happened.
    /// </summary>
    /// <remarks>
    /// A missing toast service is not an error: the component is usable without one, and an
    /// app that never registered toasts has said as much. Copying still works and reports
    /// through the button.
    /// </remarks>
    private void NotifyCopied ()
    {
        if (!NotifyOnCopy)
        {
            return;
        }

        if (Services.GetService(typeof(IVetoToastService)) is not IVetoToastService toasts)
        {
            return;
        }

        if (_copied)
        {
            toasts.Success(CopyToastMessage);
        }
        else
        {
            toasts.Error(CopyFailedToastMessage);
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