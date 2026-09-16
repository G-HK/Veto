using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Veto.Components.Toast;
using Veto.Core;

namespace Veto.Components.Code;

/// <summary>
/// A block of source code, monospaced and horizontally scrollable, with an optional copy
/// button and language label.
/// </summary>
/// <remarks>
/// Code is rendered as text, never as markup, so a snippet containing angle brackets - which
/// Razor examples always do - displays rather than executes.
/// </remarks>
public partial class VetoCode : VetoComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _copied;
    private CancellationTokenSource? _resetCopied;

    /// <summary>
    /// Resolved through the provider rather than injected directly so the component still
    /// works in an app that never registered toasts. [Inject] on IVetoToastService would throw
    /// at first render there, which is a hard failure for an entirely optional convenience.
    /// </summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc />
    protected override string RootClass => "veto-code";

    /// <summary>The code to display.</summary>
    [Parameter]
    public string? Code { get; set; }

    /// <summary>Code supplied as content instead of through <see cref="Code"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The language label shown in the bar, e.g. <c>razor</c>. Null hides the label.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>Whether to show a copy button. True by default.</summary>
    [Parameter]
    public bool Copyable { get; set; } = true;

    /// <summary>
    /// Whether a copy raises a toast. Needs a <c>VetoToastHost</c> in the layout and
    /// <c>AddVetoToast()</c> registered; without either, copying still works and says nothing.
    /// </summary>
    [Parameter]
    public bool NotifyOnCopy { get; set; } = true;

    /// <summary>What the toast says after a successful copy.</summary>
    [Parameter]
    public string CopyToastMessage { get; set; } = "Code copied to clipboard";

    /// <summary>
    /// What the toast says when the browser refused the write - outside a secure context, or
    /// with the clipboard permission denied.
    /// </summary>
    [Parameter]
    public string CopyFailedToastMessage { get; set; } = "Could not copy to the clipboard";

    /// <summary>Caps the height and scrolls, for long listings. Null means no cap.</summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-code-max-height", MaxHeight);

    private async Task CopyAsync ()
    {
        if (string.IsNullOrEmpty(Code))
        {
            return;
        }

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Code/veto-code.js");
            _copied = await _module.InvokeAsync<bool>("copyText", Code);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            _copied = false;
        }

        NotifyCopied();

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