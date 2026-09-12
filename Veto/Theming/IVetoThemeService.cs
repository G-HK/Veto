namespace Veto.Theming;

/// <summary>
/// Owns the app's theme: which mode is selected, whether that currently resolves to dark,
/// and the palette overrides in force. Registered by <c>AddVeto()</c> and injectable
/// anywhere - a page can flip the theme without going through a component.
/// </summary>
public interface IVetoThemeService
{
    /// <summary>The selected mode. Defaults to <see cref="VetoThemeMode.System"/>.</summary>
    VetoThemeMode Mode { get; }

    /// <summary>
    /// Whether the app is currently rendering dark. With <see cref="VetoThemeMode.System"/>
    /// this follows the operating system and changes when the OS preference does.
    /// </summary>
    bool IsDark { get; }

    /// <summary>The palette overrides in force.</summary>
    VetoTheme Theme { get; }

    /// <summary>
    /// Raised after the mode, the resolved appearance, or the palette changes - including when
    /// the operating system preference changes while in system mode.
    /// </summary>
    event Func<Task>? Changed;

    /// <summary>
    /// Reads the stored mode and the operating system preference, applies them to the document
    /// and starts listening for preference changes. Called by <see cref="VetoThemeProvider"/>
    /// after its first render; safe to call more than once.
    /// </summary>
    Task InitializeAsync ();

    /// <summary>Selects a mode, applies it to the document and persists it.</summary>
    Task SetModeAsync ( VetoThemeMode mode );

    /// <summary>
    /// Switches between light and dark. From <see cref="VetoThemeMode.System"/> this picks
    /// whichever mode is the opposite of what is currently showing.
    /// </summary>
    Task ToggleAsync ();

    /// <summary>Replaces the palette overrides.</summary>
    Task SetThemeAsync ( VetoTheme theme );
}
