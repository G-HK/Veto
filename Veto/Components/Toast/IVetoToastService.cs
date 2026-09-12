namespace Veto.Components.Toast;

/// <summary>
/// Raises toasts from anywhere in the app. Inject it; a single <see cref="VetoToastHost"/>
/// somewhere in the layout does the rendering.
/// </summary>
/// <remarks>
/// The service holds no opinion about how a toast looks or how long it stays - it collects what
/// was asked for and hands it to the host, which fills in whatever the caller left unset. That
/// is what lets an app change every toast's position or timing in one place.
/// </remarks>
/// <example>
/// <code>
/// @inject IVetoToastService Toasts
///
/// Toasts.Success("Saved");
/// Toasts.Error("Could not reach the server", new VetoToastOptions { Sticky = true });
/// </code>
/// </example>
public interface IVetoToastService
{
    /// <summary>The toasts currently on screen, oldest first.</summary>
    IReadOnlyList<VetoToastMessage> Toasts { get; }

    /// <summary>Raised whenever the set of toasts changes, so the host can re-render.</summary>
    event Action? OnChange;

    /// <summary>
    /// Raises a toast, and returns it - or returns the toast already on screen when this one was
    /// folded into it as a duplicate.
    /// </summary>
    /// <param name="message">What the toast says.</param>
    /// <param name="type">What kind of thing happened.</param>
    /// <param name="options">Anything that differs from the host's settings.</param>
    VetoToastMessage? Show ( string message, VetoToastType type = VetoToastType.Default, VetoToastOptions? options = null );

    /// <summary>Raises an informational toast.</summary>
    VetoToastMessage? Info ( string message, VetoToastOptions? options = null );

    /// <summary>Raises a success toast.</summary>
    VetoToastMessage? Success ( string message, VetoToastOptions? options = null );

    /// <summary>Raises a warning toast.</summary>
    VetoToastMessage? Warning ( string message, VetoToastOptions? options = null );

    /// <summary>Raises an error toast. Announced assertively rather than politely.</summary>
    VetoToastMessage? Error ( string message, VetoToastOptions? options = null );

    /// <summary>Starts one toast leaving. It stays rendered until its exit animation has run.</summary>
    /// <param name="id">The toast's <see cref="VetoToastMessage.Id"/>.</param>
    void Remove ( Guid id );

    /// <summary>Starts every toast leaving.</summary>
    void Clear ();

    /// <summary>Starts every toast of one kind leaving.</summary>
    void Clear ( VetoToastType type );
}
