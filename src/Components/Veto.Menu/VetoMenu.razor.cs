using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Menu;

/// <summary>
/// A button that drops a list of actions below it - the overflow menu on a toolbar, the
/// export choices on a chart, the account menu in an app bar.
/// </summary>
/// <remarks>
/// <para>
/// The items are ordinary composable children - <see cref="VetoMenuItem"/>,
/// <see cref="VetoMenuDivider"/> and <see cref="VetoMenuLabel"/> placed in any order - rather
/// than a list passed through a parameter, so a menu can mix actions, links and headings
/// without the component having to model each of them.
/// </para>
/// <para>
/// <see cref="OpenOn"/> chooses between a click and a hover. Hover never becomes the only way
/// in: a click still toggles the menu, Enter and the arrow keys still open it, and the panel
/// stays open while the pointer is inside it, so the menu is usable on touch and from the
/// keyboard whichever setting it has.
/// </para>
/// <para>
/// <see cref="Appearance"/> switches the items between an icon with its text, the icon alone,
/// and the text alone. Icon-only keeps the text as each item's accessible name and tooltip, so
/// it is a visual saving rather than a loss of meaning.
/// </para>
/// <para>
/// Placement is resolved in CSS against the trigger and does not flip itself away from the
/// edge of the window - see <see cref="VetoMenuPlacement"/>. This is deliberate: collision
/// handling belongs to the one popover positioning engine Veto has yet to settle on, and half
/// of one here would have to be unpicked later.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoMenu Label="Export" Icon="@VetoIcons.Download" OpenOn="VetoMenuTrigger.Hover"&gt;
///     &lt;VetoMenuItem Icon="@VetoIcons.Document" Text="CSV" OnClick="ExportCsvAsync" /&gt;
///     &lt;VetoMenuItem Icon="@VetoIcons.Copy" Text="JSON" OnClick="ExportJsonAsync" /&gt;
///     &lt;VetoMenuDivider /&gt;
///     &lt;VetoMenuItem Icon="@VetoIcons.Trash" Text="Clear" Destructive="true" OnClick="ClearAsync" /&gt;
/// &lt;/VetoMenu&gt;
/// </code>
/// </example>
public partial class VetoMenu : VetoComponentBase, IDisposable
{
    /// <summary>No item is waiting to be focused.</summary>
    private const int NoFocusRequest = -1;

    /// <summary>Focus the last item, whichever that turns out to be once the panel has rendered.</summary>
    private const int FocusLastItem = -2;

    private VetoMenuContext? _context;
    private ElementReference _triggerElement;
    private CancellationTokenSource? _hoverDelay;
    private int _activeIndex = -1;
    private int _focusRequest = NoFocusRequest;

    /// <inheritdoc />
    protected override string RootClass => "veto-menu";

    /// <summary>The items, dividers and labels in the menu.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Replaces the contents of the trigger button. The button itself - and with it
    /// <c>aria-haspopup</c>, <c>aria-expanded</c> and the keyboard handling - stays the
    /// component's, so a custom trigger cannot accidentally drop the menu's semantics.
    /// </summary>
    [Parameter]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>The trigger's text. Also the accessible name when <see cref="Appearance"/> hides it.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The trigger's icon, normally a constant from <see cref="VetoIcons"/>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// The trigger's tooltip. Falls back to <see cref="Label"/>, which is what an icon-only
    /// trigger needs and what an icon-only trigger would otherwise be missing.
    /// </summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>Whether the menu is open. Supports <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the menu opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>What opens the menu. A click by default.</summary>
    [Parameter]
    public VetoMenuTrigger OpenOn { get; set; } = VetoMenuTrigger.Click;

    /// <summary>Which corner of the trigger the panel hangs from.</summary>
    [Parameter]
    public VetoMenuPlacement Placement { get; set; } = VetoMenuPlacement.BottomStart;

    /// <summary>How much of each item is drawn: the icon and its text, the icon alone, or the text alone.</summary>
    [Parameter]
    public VetoMenuAppearance Appearance { get; set; } = VetoMenuAppearance.IconAndText;

    /// <summary>The trigger's size. The panel's text scales with it.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The trigger's visual treatment.</summary>
    [Parameter]
    public VetoVariant Variant { get; set; } = VetoVariant.Outlined;

    /// <summary>The trigger's colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Neutral;

    /// <summary>Draws the trigger as a circle. For an icon-only overflow button.</summary>
    [Parameter]
    public bool Round { get; set; }

    /// <summary>
    /// Whether the trigger shows a chevron. On by default for a trigger with text, off for an
    /// icon-only one, where a second glyph beside the first only adds noise.
    /// </summary>
    [Parameter]
    public bool? Chevron { get; set; }

    /// <summary>Packs the items tightly, for a long menu or a dense toolbar.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>Closes the menu when an item is chosen. True by default.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>The panel's minimum width as a CSS length. Keeps a short menu from looking pinched.</summary>
    [Parameter]
    public string MinWidth { get; set; } = "11rem";

    /// <summary>The gap between the trigger and the panel, as a CSS length.</summary>
    [Parameter]
    public string Offset { get; set; } = "0.375rem";

    /// <summary>
    /// How long the pointer must rest on a hover trigger before the menu opens, in
    /// milliseconds. Stops a menu flashing open as the pointer crosses the toolbar on its way
    /// somewhere else.
    /// </summary>
    [Parameter]
    public int HoverOpenDelay { get; set; } = 120;

    /// <summary>
    /// How long a hover menu stays open after the pointer leaves, in milliseconds. Long enough
    /// to forgive a pointer that clips the corner on its way to an item.
    /// </summary>
    [Parameter]
    public int HoverCloseDelay { get; set; } = 280;

    /// <summary>Disables the trigger. An open menu closes.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Raised when the menu closes, whatever closed it.</summary>
    [Parameter]
    public EventCallback OnClosed { get; set; }

    private VetoMenuContext Context => _context ??= new VetoMenuContext(this);

    private string TriggerId => $"{ElementId}-trigger";

    private string PanelId => $"{ElementId}-panel";

    private string? TooltipText => Tooltip ?? Label;

    private bool ShowChevron => Chevron ?? (Appearance != VetoMenuAppearance.IconOnly && Label is not null);

    private string PanelStyle => $"min-width:{MinWidth};";

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (Disabled && Open)
        {
            Open = false;
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier(Placement.ToModifier()))
            .AddClass(Modifier(Appearance.ToModifier()))
            .AddClass(Modifier("round"), Round)
            .AddClass(Modifier("dense"), Dense)
            .AddClass(Modifier("open"), Open)
            .AddClass(Modifier("disabled"), Disabled)
            .AddClass(Modifier("hover"), OpenOn == VetoMenuTrigger.Hover);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-menu-offset", Offset);

    /// <summary>Opens the menu.</summary>
    public Task OpenAsync () => SetOpenAsync(true);

    /// <summary>Closes the menu.</summary>
    public Task CloseAsync () => SetOpenAsync(false);

    /// <summary>Opens a closed menu, closes an open one.</summary>
    public Task ToggleAsync ()
    {
        CancelHoverDelay();

        return SetOpenAsync(!Open);
    }

    /// <summary>
    /// The trigger's click. Enter and Space on a focused button raise a click of their own, so
    /// this is the one place the keyboard and the pointer are both handled - handling the keys
    /// in <see cref="OnKeyDownAsync"/> as well opened the menu and let the click that followed
    /// close it again.
    /// </summary>
    /// <remarks>
    /// Opening from the keyboard also moves focus to the first item, which the menu-button
    /// pattern prescribes; opening with a pointer does not, because a pointer user has not
    /// asked to be moved. A click synthesised by a key carries no click count, which is what
    /// tells the two apart.
    /// </remarks>
    private async Task OnTriggerClickAsync ( MouseEventArgs args )
    {
        var opening = !Open;

        await ToggleAsync();

        if (opening && Open && args.Detail == 0)
        {
            RequestFocus(0);
        }
    }

    private async Task SetOpenAsync ( bool open )
    {
        if (Disabled && open)
        {
            return;
        }

        if (Open == open)
        {
            return;
        }

        Open = open;
        _activeIndex = -1;

        await OpenChanged.InvokeAsync(open);

        if (!open)
        {
            await OnClosed.InvokeAsync();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Called by an item that was chosen. Closes the menu unless the consumer asked it to stay,
    /// and hands focus back to the trigger so the keyboard does not end up nowhere.
    /// </summary>
    internal async Task ItemChosenAsync ()
    {
        if (!CloseOnSelect)
        {
            return;
        }

        await CloseAsync();
        await FocusTriggerAsync();
    }

    private async Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        switch (args.Key)
        {
            case "Escape":
                if (Open)
                {
                    await CloseAsync();
                    await FocusTriggerAsync();
                }

                break;

            case "Tab":
                // Let the browser move focus; the menu should not still be hanging open
                // over whatever the reader lands on next.
                if (Open)
                {
                    await CloseAsync();
                }

                break;

            case "ArrowDown":
                await MoveAsync(1);
                break;

            case "ArrowUp":
                await MoveAsync(-1);
                break;

            case "Home":
                RequestFocus(0);
                break;

            case "End":
                RequestFocus(FocusLastItem);
                break;

            case { Length: 1 } key when Open && char.IsLetterOrDigit(key[0]):
                FocusByLetter(key[0]);
                break;

        }
    }

    private async Task MoveAsync ( int delta )
    {
        if (!Open)
        {
            await OpenAsync();
            RequestFocus(delta > 0 ? 0 : FocusLastItem);

            return;
        }

        var items = FocusableItems;

        if (items.Count == 0)
        {
            return;
        }

        // Wraps, so ArrowDown on the last item returns to the first rather than dead-ending.
        var next = _activeIndex < 0
            ? (delta > 0 ? 0 : items.Count - 1)
            : (_activeIndex + delta + items.Count) % items.Count;

        RequestFocus(next);
    }

    /// <summary>
    /// Asks for an item to be focused once the panel has rendered.
    /// </summary>
    /// <remarks>
    /// Focusing cannot happen inline: a key that opens the menu does so in the same event, and
    /// at that point the panel is not in the DOM and no item has registered yet - there is
    /// nothing to focus. So the request is recorded and carried out in
    /// <see cref="OnAfterRenderAsync"/>, by which time both are true. Movement within an
    /// already-open menu goes the same way rather than taking a shortcut, so there is one path
    /// to get wrong instead of two.
    /// </remarks>
    /// <summary>
    /// Type-ahead: moves to the next item, after the current one and wrapping, whose text starts
    /// with the letter typed - the menu keyboard pattern the WAI-ARIA practices describe.
    /// Items are matched on <c>Text</c>, or <c>Label</c> when there is no text.
    /// </summary>
    private void FocusByLetter ( char letter )
    {
        var items = FocusableItems;

        for (var step = 1; step <= items.Count; step++)
        {
            var index = (_activeIndex + step + items.Count) % items.Count;
            var name = items[index].Text ?? items[index].Label;

            if (name?.TrimStart().StartsWith(letter.ToString(), StringComparison.CurrentCultureIgnoreCase) == true)
            {
                RequestFocus(index);
                return;
            }
        }
    }

    private void RequestFocus ( int target )
    {
        _focusRequest = target;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_focusRequest == NoFocusRequest)
        {
            return;
        }

        var request = _focusRequest;
        _focusRequest = NoFocusRequest;

        if (!Open)
        {
            return;
        }

        var items = FocusableItems;
        var index = request == FocusLastItem ? items.Count - 1 : request;

        if (index < 0 || index >= items.Count)
        {
            return;
        }

        _activeIndex = index;
        await items[index].FocusAsync();
    }

    /// <summary>
    /// The items the keyboard can reach: registered, rendered and not disabled. Read fresh each
    /// time rather than cached, because the panel is only in the DOM while the menu is open.
    /// </summary>
    private List<VetoMenuItem> FocusableItems =>
        Context.Items.Where(i => i.Visible && !i.Disabled).ToList();

    private async Task OnPointerEnterAsync ( PointerEventArgs args )
    {
        // A hover menu that opened for a touch "hover" would open and immediately be dismissed
        // by the tap that follows. Touch gets the click path instead.
        if (OpenOn != VetoMenuTrigger.Hover || Disabled || args.PointerType == "touch")
        {
            return;
        }

        await DelayThenAsync(HoverOpenDelay, true);
    }

    private async Task OnPointerLeaveAsync ( PointerEventArgs args )
    {
        if (OpenOn != VetoMenuTrigger.Hover || args.PointerType == "touch")
        {
            return;
        }

        await DelayThenAsync(HoverCloseDelay, false);
    }

    /// <summary>
    /// Waits out the hover delay, then opens or closes - unless the pointer changed its mind in
    /// the meantime, which cancels the pending change rather than queueing a second one.
    /// </summary>
    private async Task DelayThenAsync ( int delay, bool open )
    {
        CancelHoverDelay();

        if (Open == open)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _hoverDelay = cancellation;

        try
        {
            if (delay > 0)
            {
                await Task.Delay(delay, cancellation.Token);
            }

            if (!cancellation.IsCancellationRequested)
            {
                await SetOpenAsync(open);
            }
        }
        catch (OperationCanceledException)
        {
            // The pointer left before the menu opened, or came back before it closed.
        }
    }

    private void CancelHoverDelay ()
    {
        if (_hoverDelay is null)
        {
            return;
        }

        _hoverDelay.Cancel();
        _hoverDelay.Dispose();
        _hoverDelay = null;
    }

    private async Task FocusTriggerAsync ()
    {
        try
        {
            await _triggerElement.FocusAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // The trigger has gone - the whole menu was removed while it was open. Nothing to
            // hand focus back to.
        }
    }

    /// <inheritdoc />
    public void Dispose () => CancelHoverDelay();
}