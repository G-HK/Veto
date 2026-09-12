namespace Veto.Components.Toast;

/// <summary>
/// One live toast: what it says, what it was asked to do, and where it is in its short life.
/// Created by <see cref="VetoToastService"/> and rendered by <see cref="VetoToastHost"/>.
/// </summary>
public sealed class VetoToastMessage
{
    /// <summary>Identity for the whole life of the toast, used as the render key and for dismissal.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>What the toast says.</summary>
    public required string Message { get; init; }

    /// <summary>What kind of thing happened.</summary>
    public VetoToastType Type { get; init; } = VetoToastType.Default;

    /// <summary>Everything the caller asked for.</summary>
    public required VetoToastOptions Options { get; init; }

    /// <summary>When it was raised.</summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// How many times this same toast has been raised. Stays at one unless duplicates are set to
    /// <see cref="VetoToastDuplicates.Replace"/>, which folds a repeat into the toast already on
    /// screen and counts it here.
    /// </summary>
    public int Count { get; internal set; } = 1;

    /// <summary>
    /// True once the toast has been told to go. It stays rendered for the length of its exit
    /// animation, then leaves for good - removing it outright would cut the animation off.
    /// </summary>
    public bool Leaving { get; internal set; }

    /// <summary>True once the toast has been rendered at least once, which is what starts its entry animation.</summary>
    internal bool Entered { get; set; }

    /// <summary>True while the pointer is over the toast and its countdown is held.</summary>
    internal bool Paused { get; set; }

    /// <summary>Cancels the toast's own auto-dismiss timer.</summary>
    internal CancellationTokenSource? Timer { get; set; }

    /// <summary>
    /// The key two toasts are compared on for duplicate handling: what the caller supplied, or
    /// the type, title and message together.
    /// </summary>
    public string DeduplicationKey
        => Options.DeduplicationKey ?? $"{Type}|{Options.Title}|{Message}";

    /// <summary>True when this toast will never hide itself.</summary>
    public bool IsSticky => Options.Sticky;
}
