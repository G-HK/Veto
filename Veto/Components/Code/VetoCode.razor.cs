using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-code.js");
            _copied = await _module.InvokeAsync<bool>("copyText", Code);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            _copied = false;
        }

        if (_copied)
        {
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