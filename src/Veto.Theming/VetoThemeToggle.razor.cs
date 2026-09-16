using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Theming;

/// <summary>
/// Switches the app between light and dark. Renders either a two-state switch or, with
/// <see cref="ShowSystemOption"/>, a three-way Light / System / Dark segmented control.
/// </summary>
public partial class VetoThemeToggle : VetoComponentBase, IDisposable
{
    private static readonly VetoThemeMode[] Modes =
        [VetoThemeMode.Light, VetoThemeMode.System, VetoThemeMode.Dark];

    private bool _subscribed;

    /// <inheritdoc />
    protected override string RootClass => "veto-theme-toggle";

    /// <summary>
    /// When true, renders Light / System / Dark as a segmented control so the user can hand
    /// control back to the operating system. When false (the default) it is a plain switch
    /// between light and dark.
    /// </summary>
    [Parameter]
    public bool ShowSystemOption { get; set; }

    /// <summary>The control's accessible name.</summary>
    [Parameter]
    public string Label { get; set; } = "Toggle dark mode";

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("segmented"), ShowSystemOption)
            .AddClass(Modifier("dark"), ThemeService.IsDark);

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        ThemeService.Changed += OnThemeChangedAsync;
        _subscribed = true;
    }

    private Task ToggleAsync () => ThemeService.ToggleAsync();

    private Task SetModeAsync ( VetoThemeMode mode ) => ThemeService.SetModeAsync(mode);

    private Task OnThemeChangedAsync () => InvokeAsync(StateHasChanged);

    private static string ModeLabel ( VetoThemeMode mode ) => mode switch
    {
        VetoThemeMode.Light => "Light",
        VetoThemeMode.Dark => "Dark",
        _ => "System"
    };

    /// <summary>
    /// Inline SVG rather than an icon dependency: the toggle is often the first Veto
    /// component an app renders, and it should not require an icon set to be chosen first.
    /// </summary>
    private static RenderFragment ModeIcon ( VetoThemeMode mode ) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "veto-theme-toggle__icon");
        builder.AddAttribute(2, "viewBox", "0 0 24 24");
        builder.AddAttribute(3, "width", "16");
        builder.AddAttribute(4, "height", "16");
        builder.AddAttribute(5, "fill", "none");
        builder.AddAttribute(6, "stroke", "currentColor");
        builder.AddAttribute(7, "stroke-width", "2");
        builder.AddAttribute(8, "stroke-linecap", "round");
        builder.AddAttribute(9, "stroke-linejoin", "round");
        builder.AddAttribute(10, "aria-hidden", "true");
        builder.AddMarkupContent(11, IconPath(mode));
        builder.CloseElement();
    };

    private static string IconPath ( VetoThemeMode mode ) =>
        mode switch
        {
            // Crescent moon.
            VetoThemeMode.Dark => "<path d=\"M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z\" />",

            // Monitor.
            VetoThemeMode.System => "<rect x=\"2\" y=\"4\" width=\"20\" height=\"13\" rx=\"2\" />" +
                                    "<path d=\"M8 21h8M12 17v4\" />",

            // Sun.
            _ => "<circle cx=\"12\" cy=\"12\" r=\"4\" />" +
                 "<path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\" />"
        };

    /// <inheritdoc />
    public void Dispose ()
    {
        if (_subscribed)
        {
            ThemeService.Changed -= OnThemeChangedAsync;
        }
    }
}