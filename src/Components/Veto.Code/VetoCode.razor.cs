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

    /// <summary>
    /// Numbers each line in a gutter. Needs <see cref="Code"/>: content supplied through
    /// <see cref="ChildContent"/> is markup, not lines, and is shown as it is.
    /// </summary>
    [Parameter]
    public bool ShowLineNumbers { get; set; }

    /// <summary>The number given to the first line, for an excerpt from the middle of a file.</summary>
    [Parameter]
    public int FirstLineNumber { get; set; } = 1;

    /// <summary>
    /// Lines to highlight, as a comma-separated list of numbers and ranges - <c>"2, 5-7"</c>.
    /// Numbers are the displayed ones, so they follow <see cref="FirstLineNumber"/>. Needs
    /// <see cref="Code"/>.
    /// </summary>
    [Parameter]
    public string? HighlightLines { get; set; }

    /// <summary>When true long lines wrap instead of scrolling sideways.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    private ElementReference _codeElement;
    private IReadOnlyList<(string Text, int Number)> _lines = [];
    private HashSet<int> _highlighted = [];

    private bool UsesLines => Code is not null && (ShowLineNumbers || _highlighted.Count > 0);

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        _highlighted = ParseLineSet(HighlightLines);

        var lines = (Code ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        // A trailing newline ends the last line; it does not start an empty one.
        var count = lines.Length > 1 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
        _lines = [.. lines.Take(count).Select(( text, i ) => (text, FirstLineNumber + i))];
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("numbered"), ShowLineNumbers && Code is not null)
            .AddClass(Modifier("wrap"), Wrap);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-code-max-height", MaxHeight)
            .AddStyle("--veto-code-gutter", ShowLineNumbers ? $"{(FirstLineNumber + _lines.Count - 1).ToString().Length}ch" : null);

    private string LineClass ( int number )
        => _highlighted.Contains(number) ? "veto-code__line veto-code__line--highlight" : "veto-code__line";

    /// <summary>Parses <c>"2, 5-7"</c> into {2, 5, 6, 7}. Anything unreadable is skipped.</summary>
    private static HashSet<int> ParseLineSet ( string? spec )
    {
        var set = new HashSet<int>();

        foreach (var part in (spec ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var range = part.Split('-', 2, StringSplitOptions.TrimEntries);

            if (range.Length == 1 && int.TryParse(range[0], out var single))
            {
                set.Add(single);
            }
            else if (range.Length == 2 && int.TryParse(range[0], out var from) && int.TryParse(range[1], out var to))
            {
                // ponytail: capped so "1-99999999" cannot allocate a huge set.
                for (var n = Math.Min(from, to); n <= Math.Max(from, to) && set.Count < 10_000; n++)
                {
                    set.Add(n);
                }
            }
        }

        return set;
    }

    private async Task CopyAsync ()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Code/veto-code.js");

            // Code passed as ChildContent has no string to copy, so the browser reads it back
            // off the element instead.
            _copied = Code is not null
                ? await _module.InvokeAsync<bool>("copyText", Code)
                : await _module.InvokeAsync<bool>("copyElementText", _codeElement);
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