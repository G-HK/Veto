namespace Veto.Theming;

/// <summary>What decides whether the app renders light or dark.</summary>
public enum VetoThemeMode
{
    /// <summary>Follow the operating system's <c>prefers-color-scheme</c>, and react when it changes.</summary>
    System,

    /// <summary>Always light, whatever the operating system says.</summary>
    Light,

    /// <summary>Always dark, whatever the operating system says.</summary>
    Dark
}