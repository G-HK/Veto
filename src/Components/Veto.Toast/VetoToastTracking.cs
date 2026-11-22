namespace Veto.Components.Toast;

/// <summary>Toasts that follow a piece of work from start to finish.</summary>
public static class VetoToastTracking
{
    /// <summary>
    /// Shows <paramref name="working"/> as a sticky toast while <paramref name="task"/> runs,
    /// then replaces it with <paramref name="success"/>, or with <paramref name="error"/> if the
    /// task throws. The exception is rethrown, so the caller still handles it.
    /// </summary>
    /// <remarks>
    /// An extension rather than a member of <see cref="IVetoToastService"/>, so an app's own
    /// implementation of the interface keeps compiling.
    /// </remarks>
    /// <example>
    /// <code>await Toasts.TrackAsync(SaveAsync(), "Saving…", "Saved", "Could not save");</code>
    /// </example>
    public static async Task TrackAsync (
        this IVetoToastService toasts,
        Task task,
        string working,
        string success,
        string error,
        VetoToastOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(toasts);
        ArgumentNullException.ThrowIfNull(task);

        var pending = toasts.Info(working, Copy(options, sticky: true));

        try
        {
            await task;
        }
        catch
        {
            Finish();
            toasts.Error(error, Copy(options, sticky: false));
            throw;
        }

        Finish();
        toasts.Success(success, Copy(options, sticky: false));

        void Finish ()
        {
            if (pending is not null)
            {
                toasts.Remove(pending.Id);
            }
        }
    }

    /// <summary>
    /// Like <see cref="TrackAsync(IVetoToastService, Task, string, string, string, VetoToastOptions?)"/>,
    /// with the success message worked out from the task's result.
    /// </summary>
    public static async Task<T> TrackAsync<T> (
        this IVetoToastService toasts,
        Task<T> task,
        string working,
        Func<T, string> success,
        string error,
        VetoToastOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(toasts);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(success);

        var pending = toasts.Info(working, Copy(options, sticky: true));
        T result;

        try
        {
            result = await task;
        }
        catch
        {
            Finish();
            toasts.Error(error, Copy(options, sticky: false));
            throw;
        }

        Finish();
        toasts.Success(success(result), Copy(options, sticky: false));
        return result;

        void Finish ()
        {
            if (pending is not null)
            {
                toasts.Remove(pending.Id);
            }
        }
    }

    // Each toast gets its own options object: the service keeps a reference to it, so sharing
    // one between the working and the finished toast would make the finished one sticky too.
    private static VetoToastOptions Copy ( VetoToastOptions? source, bool sticky ) => new()
    {
        Title = source?.Title,
        Position = source?.Position,
        Animation = source?.Animation,
        Duration = source?.Duration,
        Sticky = sticky,
        ShowProgress = source?.ShowProgress,
        ShowCloseButton = source?.ShowCloseButton,
        CloseOnClick = source?.CloseOnClick,
        PauseOnHover = source?.PauseOnHover,
        Class = source?.Class,
        Style = source?.Style,
        TestId = source?.TestId,
        Size = source?.Size
    };
}
