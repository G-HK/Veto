namespace Veto.Components.Toast;

/// <summary>
/// The default <see cref="IVetoToastService"/>. Holds the live toasts and tells the host when
/// the set changes.
/// </summary>
/// <remarks>
/// <para>
/// Registered as scoped by <c>AddVeto</c>, so each circuit - or each WebAssembly app - has its
/// own queue. A singleton would show one user's toasts to every other user on the server.
/// </para>
/// <para>
/// The service deliberately knows nothing about timing or animation. It does not start the
/// auto-dismiss timers, because the caller's <c>Duration</c> is allowed to be null and only the
/// host knows what null means; and it does not remove a leaving toast, because only the host
/// knows how long the exit animation runs. It owns the list; the host owns the clock.
/// </para>
/// </remarks>
public sealed class VetoToastService : IVetoToastService
{
    private readonly List<VetoToastMessage> _toasts = [];

    private readonly Lock _gate = new();

    /// <summary>
    /// The most toasts that may exist at once, set by the host from its own <c>MaxToasts</c>.
    /// Raising one beyond the limit retires the oldest that is not sticky.
    /// </summary>
    internal int MaxToasts { get; set; } = 5;

    /// <summary>The host's duplicate policy, used for any toast that does not state its own.</summary>
    internal VetoToastDuplicates Duplicates { get; set; } = VetoToastDuplicates.Allow;

    /// <inheritdoc />
    public IReadOnlyList<VetoToastMessage> Toasts
    {
        get
        {
            lock (_gate)
            {
                return [.. _toasts];
            }
        }
    }

    /// <inheritdoc />
    public event Action? OnChange;

    /// <inheritdoc />
    public VetoToastMessage? Show ( string message, VetoToastType type = VetoToastType.Default, VetoToastOptions? options = null )
    {
        var toast = new VetoToastMessage
        {
            Message = message ?? string.Empty,
            Type = type,
            Options = options ?? new VetoToastOptions(),
        };

        VetoToastMessage? result;

        lock (_gate)
        {
            var policy = toast.Options.Duplicates ?? Duplicates;

            if (policy != VetoToastDuplicates.Allow)
            {
                var existing = _toasts.FirstOrDefault(
                    t => !t.Leaving && t.DeduplicationKey == toast.DeduplicationKey);

                if (existing is not null)
                {
                    if (policy == VetoToastDuplicates.Replace)
                    {
                        // The repeat is the information, so the count goes up and the timer is
                        // restarted by the host when it sees the change.
                        existing.Count++;
                        existing.Timer?.Cancel();
                        existing.Timer = null;
                    }

                    result = existing;
                    goto notify;
                }
            }

            // Over the limit, the oldest toast makes way - but never a sticky one, which is on
            // screen precisely because it must not disappear on its own.
            var live = _toasts.Where(t => !t.Leaving).ToList();

            if (MaxToasts > 0 && live.Count >= MaxToasts)
            {
                foreach (var stale in live.Take(live.Count - MaxToasts + 1).Where(t => !t.IsSticky))
                {
                    stale.Leaving = true;
                }
            }

            _toasts.Add(toast);
            result = toast;
        }

    notify:
        OnChange?.Invoke();
        return result;
    }

    /// <inheritdoc />
    public VetoToastMessage? Info ( string message, VetoToastOptions? options = null )
        => Show(message, VetoToastType.Info, options);

    /// <inheritdoc />
    public VetoToastMessage? Success ( string message, VetoToastOptions? options = null )
        => Show(message, VetoToastType.Success, options);

    /// <inheritdoc />
    public VetoToastMessage? Warning ( string message, VetoToastOptions? options = null )
        => Show(message, VetoToastType.Warning, options);

    /// <inheritdoc />
    public VetoToastMessage? Error ( string message, VetoToastOptions? options = null )
        => Show(message, VetoToastType.Error, options);

    /// <inheritdoc />
    public void Remove ( Guid id )
    {
        lock (_gate)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);

            if (toast is null || toast.Leaving)
            {
                return;
            }

            toast.Leaving = true;
            toast.Timer?.Cancel();
        }

        OnChange?.Invoke();
    }

    /// <inheritdoc />
    public void Clear () => ClearWhere(_ => true);

    /// <inheritdoc />
    public void Clear ( VetoToastType type ) => ClearWhere(t => t.Type == type);

    /// <summary>Drops a toast for good once its exit animation has run. Called by the host.</summary>
    internal void Discard ( Guid id )
    {
        lock (_gate)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);

            if (toast is null)
            {
                return;
            }

            toast.Timer?.Cancel();
            _toasts.Remove(toast);
        }

        OnChange?.Invoke();
    }

    private void ClearWhere ( Func<VetoToastMessage, bool> predicate )
    {
        lock (_gate)
        {
            foreach (var toast in _toasts.Where(t => !t.Leaving && predicate(t)))
            {
                toast.Leaving = true;
                toast.Timer?.Cancel();
            }
        }

        OnChange?.Invoke();
    }
}
