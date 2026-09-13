using Microsoft.AspNetCore.Components;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Toast;

/// <summary>
/// Renders every toast <see cref="IVetoToastService"/> raises. Place one in the app's layout -
/// once, near the root - and nothing else has to know toasts exist.
/// </summary>
/// <remarks>
/// <para>
/// The host holds the defaults and the clock. A caller may override any of them per toast, so
/// the settings here answer "what does this app's toast look like", not "what does this toast
/// look like".
/// </para>
/// <para>
/// Toasts are grouped into one stack per position, so an app that normally toasts bottom-right
/// can still put one message top-centre without the two fighting over the same corner.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoToastHost Position="VetoToastPosition.BottomRight"
///                Animation="VetoToastAnimation.SlideRight"
///                MaxToasts="3"
///                Duplicates="VetoToastDuplicates.Replace" /&gt;
/// </code>
/// </example>
public partial class VetoToastHost : VetoComponentBase, IDisposable
{
    private const int TickMilliseconds = 100;

    private readonly Dictionary<Guid, CancellationTokenSource> _exits = [];

    /// <inheritdoc />
    protected override string RootClass => "veto-toast-stack";

    /// <inheritdoc />
    /// <remarks>The stack is fixed to a corner of the viewport. Letting <c>ZIndex</c> make it relative
    /// would drop it back into the flow.</remarks>
    protected override bool PositionsItself => true;

    /// <summary>Where toasts go unless one asks for somewhere else.</summary>
    [Parameter]
    public VetoToastPosition Position { get; set; } = VetoToastPosition.TopRight;

    /// <summary>How toasts arrive and leave unless one asks for something else.</summary>
    [Parameter]
    public VetoToastAnimation Animation { get; set; } = VetoToastAnimation.Fade;

    /// <summary>How long a toast stays, in milliseconds, unless it says otherwise.</summary>
    [Parameter]
    public int Duration { get; set; } = 5000;

    /// <summary>
    /// How long the arrival and departure animations run, in milliseconds. Also how long a
    /// dismissed toast stays in the DOM, since removing it any sooner would cut its exit short.
    /// </summary>
    [Parameter]
    public int AnimationDuration { get; set; } = 250;

    /// <summary>
    /// The most toasts on screen at once. Raising one beyond the limit retires the oldest that
    /// is not sticky. Zero means no limit.
    /// </summary>
    [Parameter]
    public int MaxToasts { get; set; } = 5;

    /// <summary>
    /// What happens when a toast is raised whose message is already on screen: show it anyway,
    /// drop it, or fold it into the existing one and count the repeat.
    /// </summary>
    [Parameter]
    public VetoToastDuplicates Duplicates { get; set; } = VetoToastDuplicates.Allow;

    /// <summary>Whether a bar counts each toast's remaining time down. Never drawn on a sticky toast.</summary>
    [Parameter]
    public bool ShowProgress { get; set; } = true;

    /// <summary>Whether toasts carry a close button.</summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Whether clicking anywhere on a toast dismisses it.</summary>
    [Parameter]
    public bool CloseOnClick { get; set; }

    /// <summary>
    /// Whether the countdown holds while the pointer is over a toast. On by default, so a toast
    /// is never snatched away mid-read.
    /// </summary>
    [Parameter]
    public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// Whether the newest toast sits nearest the screen edge. On by default: the newest message
    /// is the one worth reading first.
    /// </summary>
    [Parameter]
    public bool NewestOnTop { get; set; } = true;

    /// <summary>The size of a toast.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>
    /// When false a toast drops its outline and its type stripe, leaning on the shadow alone to
    /// lift it off the page. The icon keeps the type's colour, so what kind of message it is
    /// survives losing the stripe.
    /// </summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>The accessible name of each stack region.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Notifications";

    /// <summary>The accessible name of a toast's close button.</summary>
    [Parameter]
    public string CloseLabel { get; set; } = "Dismiss";

    /// <summary>The live toasts, grouped into one stack per position and ordered for that corner.</summary>
    private IEnumerable<KeyValuePair<VetoToastPosition, List<VetoToastMessage>>> Stacks
    {
        get
        {
            var grouped = new Dictionary<VetoToastPosition, List<VetoToastMessage>>();

            foreach (var toast in Service.Toasts)
            {
                var position = toast.Options.Position ?? Position;

                if (!grouped.TryGetValue(position, out var list))
                {
                    grouped[position] = list = [];
                }

                list.Add(toast);
            }

            foreach (var (position, list) in grouped)
            {
                // The list is oldest first. Newest-on-top means newest nearest the screen edge,
                // which is the start of the stack at the top and the end of it at the bottom -
                // the CSS reverses bottom stacks, so only the top ones flip here.
                if (NewestOnTop && position is VetoToastPosition.TopLeft or VetoToastPosition.TopCenter or VetoToastPosition.TopRight)
                {
                    list.Reverse();
                }
                else if (!NewestOnTop && position is VetoToastPosition.BottomLeft or VetoToastPosition.BottomCenter or VetoToastPosition.BottomRight)
                {
                    list.Reverse();
                }
            }

            return grouped;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        Service.OnChange += HandleChange;
    }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        // The service needs these two to decide what to do at the moment a toast is raised, which
        // is before the host gets a look at it.
        if (Service is VetoToastService concrete)
        {
            concrete.MaxToasts = MaxToasts;
            concrete.Duplicates = Duplicates;
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender ( bool firstRender )
    {
        var again = false;

        foreach (var toast in Service.Toasts)
        {
            if (toast.Leaving)
            {
                ScheduleDiscard(toast);
                continue;
            }

            if (!toast.Entered)
            {
                // The toast was rendered once in its "entering" state; flipping the flag now is
                // what gives the CSS transition two states to move between. Without the pair of
                // renders there is nothing to animate and the toast simply appears.
                toast.Entered = true;
                again = true;
            }

            if (!toast.IsSticky && toast.Timer is null)
            {
                StartTimer(toast);
            }
        }

        if (again)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier("borderless"), !Bordered);

    /// <summary>Releases the timers and stops listening to the service.</summary>
    public void Dispose ()
    {
        Service.OnChange -= HandleChange;

        foreach (var toast in Service.Toasts)
        {
            toast.Timer?.Cancel();
            toast.Timer = null;
        }

        foreach (var exit in _exits.Values)
        {
            exit.Cancel();
        }

        _exits.Clear();
    }

    private void HandleChange () => _ = InvokeAsync(StateHasChanged);

    private string StackClass ( VetoToastPosition position )
        => CssBuilder.Default(CssClass).AddClass(Modifier(Slug(position))).Build() ?? RootClass;

    private string? ToastClass ( VetoToastMessage toast ) =>
        CssBuilder.Default("veto-toast")
            .AddClass($"veto-toast--{Slug(toast.Type)}")
            .AddClass($"veto-toast--{Resolve(toast.Options.Size, Size).ToModifier()}")
            .AddClass("veto-toast--clickable", Resolve(toast.Options.CloseOnClick, CloseOnClick) || toast.Options.OnClick is not null)
            .AddClass("veto-toast--paused", Resolve(toast.Options.PauseOnHover, PauseOnHover))
            .AddClass(toast.Options.Class)
            .Build();

    private string ToastStyle ( VetoToastMessage toast ) =>
        $"--veto-toast-animation-duration:{AnimationDuration}ms;{toast.Options.Style}";

    private bool ShowsProgress ( VetoToastMessage toast ) =>
        !toast.IsSticky && Resolve(toast.Options.ShowProgress, ShowProgress);

    private int ResolveDuration ( VetoToastMessage toast )
    {
        var duration = toast.Options.Duration ?? Duration;
        return duration > 0 ? duration : Duration;
    }

    private static T Resolve<T> ( T? supplied, T fallback ) where T : struct =>
        supplied ?? fallback;

    private static string Slug ( Enum value )
    {
        var name = value.ToString();
        var slug = new System.Text.StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                slug.Append('-');
            }

            slug.Append(char.ToLowerInvariant(name[i]));
        }

        return slug.ToString();
    }

    private static string? IconFor ( VetoToastMessage toast ) => toast.Options.Icon ?? toast.Type switch
    {
        VetoToastType.Info => VetoIcons.Info,
        VetoToastType.Success => VetoIcons.Success,
        VetoToastType.Warning => VetoIcons.Warning,
        VetoToastType.Error => VetoIcons.Error,
        _ => null
    };

    private void SetPaused ( VetoToastMessage toast, bool paused )
    {
        if (Resolve(toast.Options.PauseOnHover, PauseOnHover))
        {
            toast.Paused = paused;
        }
    }

    private async Task OnToastClickAsync ( VetoToastMessage toast )
    {
        if (toast.Options.OnClick is { } handler)
        {
            await handler();
        }

        if (Resolve(toast.Options.CloseOnClick, CloseOnClick))
        {
            Dismiss(toast);
        }
    }

    private void Dismiss ( VetoToastMessage toast ) => Service.Remove(toast.Id);

    private void StartTimer ( VetoToastMessage toast )
    {
        var cts = new CancellationTokenSource();
        toast.Timer = cts;

        _ = RunTimerAsync(toast, ResolveDuration(toast), cts.Token);
    }

    private async Task RunTimerAsync ( VetoToastMessage toast, int duration, CancellationToken token )
    {
        try
        {
            var remaining = duration;

            // Ticked rather than awaited in one go so hovering can hold the countdown - the CSS
            // progress bar pauses the same way, so the two stay in step.
            while (remaining > 0)
            {
                await Task.Delay(TickMilliseconds, token);

                if (!toast.Paused)
                {
                    remaining -= TickMilliseconds;
                }
            }

            Service.Remove(toast.Id);
        }
        catch (OperationCanceledException)
        {
            // The toast was dismissed, replaced or the host was torn down.
        }
    }

    private void ScheduleDiscard ( VetoToastMessage toast )
    {
        if (_exits.ContainsKey(toast.Id))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _exits[toast.Id] = cts;

        _ = DiscardAsync(toast, cts.Token);
    }

    private async Task DiscardAsync ( VetoToastMessage toast, CancellationToken token )
    {
        try
        {
            await Task.Delay(AnimationDuration, token);

            if (Service is VetoToastService concrete)
            {
                concrete.Discard(toast.Id);
            }

            if (toast.Options.OnClose is { } handler)
            {
                await handler();
            }
        }
        catch (OperationCanceledException)
        {
            // The host went away before the exit animation finished.
        }
        finally
        {
            _exits.Remove(toast.Id);
        }
    }
}