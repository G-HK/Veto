using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Buttons;

/// <summary>
/// One button that unfolds into several when the pointer reaches it - the compact form of a
/// toolbar that would otherwise take a whole row. The revealed actions are ordinary
/// <see cref="VetoIconButton"/> children, so they behave exactly as they would anywhere else.
/// </summary>
/// <remarks>
/// <para>
/// Hover alone would put the actions out of reach of a keyboard, so focus reveals them too:
/// tabbing to the trigger opens the cluster, Tab then walks into the actions, and focus
/// leaving closes it. On touch, where there is no hover, a tap on the trigger opens it. The
/// actions are therefore reachable three ways and the reveal never becomes the only route.
/// </para>
/// <para>
/// <see cref="OnClick"/> changes what a click does. Without it the trigger is a disclosure and
/// clicking toggles the cluster. With it the trigger is a real action - "Export" that exports
/// the default format - and the cluster is the shortcut to the variants, so the click runs the
/// action and the reveal is left to the pointer and the keyboard.
/// </para>
/// <para>
/// While closed the actions are <c>inert</c> rather than <c>display:none</c>: they stay in the
/// layout, so the reveal can be a transition rather than a jump, but they take no focus and
/// are not announced.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoButtonReveal Icon="@VetoIcons.Share" Label="Share"&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.GitHub" Label="GitHub" /&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.X" Label="X" /&gt;
///     &lt;VetoIconButton Icon="@VetoIcons.Mail" Label="Email" /&gt;
/// &lt;/VetoButtonReveal&gt;
/// </code>
/// </example>
public partial class VetoButtonReveal : VetoComponentBase, IDisposable
{
    private ElementReference _triggerElement;
    private CancellationTokenSource? _pending;
    private DateTime _reopenBlockedUntil = DateTime.MinValue;

    /// <inheritdoc />
    protected override string RootClass => "veto-button-reveal";

    /// <summary>The buttons the trigger unfolds into. Normally <see cref="VetoIconButton"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Replaces the contents of the trigger button. The button element stays the component's,
    /// so <c>aria-expanded</c> and the keyboard handling cannot be lost with it.
    /// </summary>
    [Parameter]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>The trigger's icon, normally a constant from <see cref="VetoIcons"/>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// The trigger's accessible name, shown as text when <see cref="ShowLabel"/> is on and used
    /// as the cluster's group label. Always supply one.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Whether the trigger's label is drawn beside its icon. Off by default: the point of the
    /// component is a cluster that takes one button's worth of room until it is needed.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; }

    /// <summary>The trigger's tooltip. Falls back to <see cref="Label"/>.</summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>
    /// The revealed group's accessible name, when it should differ from <see cref="Label"/> -
    /// "Export formats" under a trigger labelled "Export".
    /// </summary>
    [Parameter]
    public string? ActionsLabel { get; set; }

    /// <summary>Whether the actions are showing. Supports <c>@bind-Expanded</c>.</summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Raised when the cluster opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>What reveals the actions. Hover by default, with focus and touch alongside it.</summary>
    [Parameter]
    public VetoRevealTrigger OpenOn { get; set; } = VetoRevealTrigger.Hover;

    /// <summary>Which way the actions unfold.</summary>
    [Parameter]
    public VetoRevealDirection Direction { get; set; } = VetoRevealDirection.End;

    /// <summary>The trigger's size. The revealed buttons keep their own.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The trigger's visual treatment.</summary>
    [Parameter]
    public VetoVariant Variant { get; set; } = VetoVariant.Filled;

    /// <summary>The trigger's colour role.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>Draws the trigger as a circle. The speed-dial look.</summary>
    [Parameter]
    public bool Round { get; set; }

    /// <summary>The gap between the revealed buttons.</summary>
    [Parameter]
    public VetoSpacing Gap { get; set; } = VetoSpacing.ExtraSmall;

    /// <summary>
    /// Whether each action arrives a moment after the one before it, so the cluster unrolls
    /// rather than appearing whole. Ignored by a reader who asked for reduced motion.
    /// </summary>
    [Parameter]
    public bool Stagger { get; set; } = true;

    /// <summary>
    /// How long the pointer must be on the cluster before it opens, in milliseconds. Zero by
    /// default: unlike a menu, the actions appear beside the trigger rather than over the page,
    /// so opening early costs the reader nothing.
    /// </summary>
    [Parameter]
    public int OpenDelay { get; set; }

    /// <summary>
    /// How long the cluster stays open after the pointer leaves, in milliseconds. Long enough
    /// that crossing the gap between the trigger and the last action does not close it.
    /// </summary>
    [Parameter]
    public int CloseDelay { get; set; } = 240;

    /// <summary>Disables the trigger and closes the cluster. The actions keep their own state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Makes the trigger a real action rather than a disclosure. When set, a click runs this
    /// instead of toggling the cluster - see the remarks on <see cref="VetoButtonReveal"/>.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string TriggerId => $"{ElementId}-trigger";

    private string ActionsId => $"{ElementId}-actions";

    private string? TooltipText => Tooltip ?? Label;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (Disabled && Expanded)
        {
            Expanded = false;
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier(Direction.ToModifier()))
            .AddClass(Modifier(Direction.IsInline() ? "inline" : "block"))
            .AddClass(Modifier("round"), Round)
            .AddClass(Modifier("labelled"), ShowLabel)
            .AddClass(Modifier("open"), Expanded)
            .AddClass(Modifier("stagger"), Stagger)
            .AddClass(Modifier("disabled"), Disabled);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle () =>
        base.StartStyle()
            .AddStyle("--veto-button-reveal-gap", GapValue);

    /// <summary>
    /// The gap as a CSS length. None has no token of its own - the spacing scale starts at
    /// <c>--veto-space-1</c> - so it resolves to a plain zero rather than to a variable that
    /// does not exist.
    /// </summary>
    private string GapValue => Gap == VetoSpacing.None
        ? "0"
        : $"var(--veto-space-{Gap.ToModifier()})";

    /// <summary>Reveals the actions.</summary>
    public Task ExpandAsync () => SetExpandedAsync(true);

    /// <summary>Folds the actions away.</summary>
    public Task CollapseAsync () => SetExpandedAsync(false);

    private async Task SetExpandedAsync ( bool expanded )
    {
        if (Disabled && expanded)
        {
            return;
        }

        if (Expanded == expanded)
        {
            return;
        }

        Expanded = expanded;
        await ExpandedChanged.InvokeAsync(expanded);
        StateHasChanged();
    }

    private async Task OnTriggerClickAsync ( MouseEventArgs args )
    {
        if (Disabled)
        {
            return;
        }

        CancelPending();

        // A trigger given its own action is not a disclosure button: it runs the action, and
        // the cluster is opened by the pointer or the keyboard instead. Without one, the click
        // is the only way in on a touch screen, so it has to toggle.
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(args);

            return;
        }

        await SetExpandedAsync(!Expanded);
    }

    private async Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        if (args.Key != "Escape" || !Expanded)
        {
            return;
        }

        // Handing focus back to the trigger raises a focusin, and focusin is what opens the
        // cluster - so dismissing it would immediately undo itself. The browser delivers that
        // focusin asynchronously, after this handler has returned, so it cannot simply be
        // ordered around; a short window in which focus does not reopen the cluster is what
        // separates our own focus move from the reader deliberately coming back to it.
        _reopenBlockedUntil = DateTime.UtcNow.AddMilliseconds(200);

        await SetExpandedAsync(false);
        await FocusTriggerAsync();
    }

    private async Task OnPointerEnterAsync ( PointerEventArgs args )
    {
        // Touch reports an enter immediately before the tap that follows, which would open the
        // cluster and then have the tap toggle it shut again. Touch uses the click path.
        if (OpenOn != VetoRevealTrigger.Hover || Disabled || args.PointerType == "touch")
        {
            return;
        }

        await DelayThenAsync(OpenDelay, true);
    }

    private async Task OnPointerLeaveAsync ( PointerEventArgs args )
    {
        if (args.PointerType == "touch")
        {
            return;
        }

        await DelayThenAsync(CloseDelay, false);
    }

    /// <summary>
    /// Keyboard focus arriving anywhere in the cluster opens it, which is what makes the
    /// actions reachable without a pointer. Only in hover mode: in click mode the click already
    /// does it, and opening on focus as well would fight the toggle - the click focuses the
    /// trigger first, so the cluster would open and immediately be closed again.
    /// </summary>
    private async Task OnFocusInAsync ( FocusEventArgs args )
    {
        CancelPending();

        if (OpenOn != VetoRevealTrigger.Hover || Disabled || DateTime.UtcNow < _reopenBlockedUntil)
        {
            return;
        }

        await SetExpandedAsync(true);
    }

    /// <summary>
    /// Focus leaving closes the cluster. Blazor's focus arguments do not say where focus went,
    /// so this waits a moment: if it moved to another button inside the cluster, the focusin
    /// that follows cancels the close before it happens.
    /// </summary>
    private Task OnFocusOutAsync ( FocusEventArgs args ) => DelayThenAsync(80, false);

    private async Task DelayThenAsync ( int delay, bool expanded )
    {
        CancelPending();

        if (Expanded == expanded)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        try
        {
            if (delay > 0)
            {
                await Task.Delay(delay, cancellation.Token);
            }

            if (!cancellation.IsCancellationRequested)
            {
                await SetExpandedAsync(expanded);
            }
        }
        catch (OperationCanceledException)
        {
            // The pointer came back, or focus landed on another action in the cluster.
        }
    }

    private void CancelPending ()
    {
        if (_pending is null)
        {
            return;
        }

        _pending.Cancel();
        _pending.Dispose();
        _pending = null;
    }

    private async Task FocusTriggerAsync ()
    {
        try
        {
            await _triggerElement.FocusAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // The cluster was removed while it was open; there is nothing to focus.
        }
    }

    /// <inheritdoc />
    public void Dispose () => CancelPending();
}