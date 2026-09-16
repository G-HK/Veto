using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Veto.Core;

namespace Veto.Components.MagicTextBox;

/// <summary>
/// A multi-line text box that keeps its place in the page when the text outgrows it: it holds
/// itself to a few lines, and offers to open when - and only when - there is more text than
/// fits. Carries corner slots for the consumer's own buttons and a copy-to-clipboard button.
/// </summary>
/// <remarks>
/// <para>
/// The toggle appears only when the text actually overflows the collapsed height, which the
/// browser has to measure - so it appears after the first render rather than during it. The
/// root advertises the result on <c>data-overflowing</c>, and the measurement is taken against
/// the collapsed height whether or not the field is currently open, so opening the field does
/// not make the control that closes it disappear.
/// </para>
/// <para>
/// A field that never overflows never grows a toggle, so a short value looks like an ordinary
/// text box. That is the intended resting state: the component earns its keep only on the
/// values that need it.
/// </para>
/// </remarks>
/// <typeparam name="TValue">
/// The bound type. <see cref="string"/> is the usual one; anything Blazor's
/// <see cref="BindConverter"/> handles works, as it does on <c>VetoTextBox</c>.
/// </typeparam>
public partial class VetoMagicTextBox<TValue> : VetoInputBase<TValue>, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<VetoMagicTextBox<TValue>>? _selfRef;
    private CancellationTokenSource? _resetCopied;
    private bool _overflowing;
    private bool _measured;
    private bool _copied;

    /// <inheritdoc />
    protected override string RootClass => "veto-magic-textbox";

    /// <summary>
    /// How many lines the field shows while closed. The measurement that decides whether a
    /// toggle is needed is taken against this height.
    /// </summary>
    [Parameter]
    public int CollapsedLines { get; set; } = 3;

    /// <summary>
    /// How many lines the field grows to when opened. Null - the default - lets it grow to fit
    /// the whole value, however long that is.
    /// </summary>
    [Parameter]
    public int? ExpandedLines { get; set; }

    /// <summary>
    /// Whether the field may be opened at all. False keeps it permanently at
    /// <see cref="CollapsedLines"/> and renders no toggle; the text still scrolls.
    /// </summary>
    [Parameter]
    public bool Expandable { get; set; } = true;

    /// <summary>Whether the field is currently open. Two-way bindable with <c>@bind-Expanded</c>.</summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Raised when the field opens or closes, with the new state.</summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>Whether to offer a copy-to-clipboard button. True by default.</summary>
    [Parameter]
    public bool Copyable { get; set; } = true;

    /// <summary>When the copy button is shown. On hover and focus by default.</summary>
    [Parameter]
    public VetoCopyReveal CopyReveal { get; set; } = VetoCopyReveal.Hover;

    /// <summary>The copy button's accessible name and tooltip, before it has been used.</summary>
    [Parameter]
    public string CopyLabel { get; set; } = "Copy text";

    /// <summary>The copy button's accessible name and tooltip for the moment after a successful copy.</summary>
    [Parameter]
    public string CopiedLabel { get; set; } = "Copied";

    /// <summary>The toggle's text while the field is closed.</summary>
    [Parameter]
    public string ExpandLabel { get; set; } = "Show more";

    /// <summary>The toggle's text while the field is open.</summary>
    [Parameter]
    public string CollapseLabel { get; set; } = "Show less";

    /// <summary>
    /// Content pinned to the field's leading corner - the consumer's own buttons. It sits over
    /// the text, and the field reserves a gutter for it so no line ever runs underneath.
    /// </summary>
    /// <remarks>
    /// A slot rather than ordinary children because the field's content is a
    /// <c>&lt;textarea&gt;</c>, which by definition cannot have element children. Corner
    /// content is always visible; only the copy button follows <see cref="CopyReveal"/>.
    /// </remarks>
    [Parameter]
    public RenderFragment? LeadingContent { get; set; }

    /// <summary>
    /// Content pinned to the field's trailing corner, before the copy button. Same reasoning
    /// as <see cref="LeadingContent"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    /// <summary>
    /// When true the bound value updates on every keystroke; when false (the default) it
    /// updates when the field loses focus, matching plain <c>@bind-Value</c>.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; }

    /// <summary>
    /// The format passed to <see cref="BindConverter"/> when converting the value to and from
    /// its string form.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Raised after the bound value changes, with the new value.</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChanged { get; set; }

    /// <summary>
    /// Raised after the copy button is used, with whether the browser accepted the write. It
    /// refuses outside a secure context, which is worth surfacing rather than swallowing.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnCopied { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("expanded"), Expanded)
            .AddClass(Modifier("expandable"), Expandable)
            .AddClass(Modifier("overflowing"), _overflowing)
            .AddClass(Modifier("copy-always"), Copyable && CopyReveal is VetoCopyReveal.Always);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-magic-textbox-collapsed-lines", CollapsedLines.ToString(CultureInfo.InvariantCulture))
            .AddStyle(
                "--veto-magic-textbox-expanded-lines",
                ExpandedLines?.ToString(CultureInfo.InvariantCulture),
                ExpandedLines.HasValue);

    private Task ToggleAsync ()
    {
        Expanded = !Expanded;

        return ExpandedChanged.HasDelegate
            ? ExpandedChanged.InvokeAsync(Expanded)
            : Task.CompletedTask;
    }

    private Task OnInputAsync ( ChangeEventArgs args ) =>
        Immediate ? SetCurrentValueAsync(args.Value?.ToString()) : Task.CompletedTask;

    private Task OnChangeAsync ( ChangeEventArgs args ) =>
        Immediate ? Task.CompletedTask : SetCurrentValueAsync(args.Value?.ToString());

    private async Task SetCurrentValueAsync ( string? value )
    {
        // CurrentValueAsString runs InputBase's parse + validation pipeline and raises
        // ValueChanged, so @bind-Value and the EditContext both stay in sync.
        CurrentValueAsString = value;

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }
    }

    /// <summary>
    /// Reports a change in whether the text outruns the collapsed height. Called from the
    /// resize observer in <c>veto-magic-textbox.js</c>.
    /// </summary>
    /// <param name="overflowing">True when the value no longer fits the collapsed height.</param>
    [JSInvokable]
    public Task OnOverflowChangedAsync ( bool overflowing )
    {
        if (_overflowing == overflowing)
        {
            return Task.CompletedTask;
        }

        _overflowing = overflowing;
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        await base.OnAfterRenderAsync(firstRender);
        await SyncOverflowAsync(firstRender);
    }

    /// <summary>
    /// Measures the value against the collapsed height and publishes the result.
    /// </summary>
    /// <remarks>
    /// Re-measured on every render, not only on the first: the observer catches the field
    /// being resized and the user typing, but a value replaced from code changes neither the
    /// element's size nor raises an input event, and the toggle would go stale.
    /// </remarks>
    private async Task SyncOverflowAsync ( bool firstRender )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Veto.MagicTextBox/veto-magic-textbox.js");

            if (firstRender)
            {
                _selfRef ??= DotNetObjectReference.Create(this);
                await _module.InvokeAsync<bool>("attach", InputElement, _selfRef, CollapsedLines);
            }

            // sync both re-measures and applies the open height, so it runs on the first
            // render too - attach only starts the observer.
            var overflowing = await _module.InvokeAsync<bool>(
                "sync", InputElement, CollapsedLines, Expanded, ExpandedLines);

            if (_measured && _overflowing == overflowing)
            {
                return;
            }

            _overflowing = overflowing;
            _measured = true;

            // Re-rendered so data-overflowing stops saying nothing and the toggle appears.
            // Measuring happens after a render, so without this the field would advertise the
            // state it had before it was measured - and anything waiting on that signal, an
            // app or a test, would wait forever. The guard above stops the next render from
            // measuring its way into a loop.
            StateHasChanged();
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or the page has gone. The field stays closed and shows no toggle,
            // which is the same thing it shows for a value that fits. Deliberately nothing
            // else: copying is a separate path with its own failure handling, and a field that
            // could not be measured is still a field whose text you may want to copy.
        }
    }

    private async Task CopyAsync ()
    {
        var text = CurrentValueAsString;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Veto.MagicTextBox/veto-magic-textbox.js");
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
    protected override string? FormatValueAsString ( TValue? value ) =>
        Format is not null && value is IFormattable formattable
            ? formattable.ToString(Format, CultureInfo.InvariantCulture)
            : base.FormatValueAsString(value);

    /// <inheritdoc />
    protected override bool TryParseValueFromString ( string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage )
    {
        var parsed = Format is null
            ? BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var converted)
            : BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out converted);

        if (parsed)
        {
            result = converted!;
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
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
                await _module.InvokeVoidAsync("detach", InputElement);
                await _module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException)
            {
                // The page is already gone; the observer went with it.
            }
        }

        _selfRef?.Dispose();
    }
}