using Microsoft.AspNetCore.Components;

namespace Veto.Theming;

/// <summary>
/// Wraps the app, boots <see cref="IVetoThemeService"/> once the page is interactive, and
/// emits the app's palette overrides as CSS custom properties. Place it around the router
/// in the app's layout - one per app.
/// </summary>
/// <remarks>
/// It renders no element of its own, so it never disturbs a layout. Appearance itself is
/// carried by the <c>data-veto-theme</c> attribute on <c>&lt;html&gt;</c>, which the service
/// maintains; this component only supplies overrides and keeps the tree re-rendering when
/// the theme changes.
/// </remarks>
public partial class VetoThemeProvider : IDisposable
{
    private string? _css;
    private bool _subscribed;

    /// <summary>The app's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Palette overrides to apply. Anything left null on the theme keeps the built-in token
    /// value from <c>VetoStyles.css</c>.
    /// </summary>
    [Parameter]
    public VetoTheme? Theme { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        ThemeService.Changed += OnThemeChangedAsync;
        _subscribed = true;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync ()
    {
        if (Theme is not null && !ReferenceEquals(Theme, ThemeService.Theme))
        {
            await ThemeService.SetThemeAsync(Theme);
        }

        _css = ThemeService.Theme.HasOverrides ? ThemeService.Theme.ToCss() : null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        // JS interop is unavailable while prerendering, so the stored mode and the OS
        // preference can only be read once the component is interactive.
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }
    }

    private async Task OnThemeChangedAsync ()
    {
        _css = ThemeService.Theme.HasOverrides ? ThemeService.Theme.ToCss() : null;
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        if (_subscribed)
        {
            ThemeService.Changed -= OnThemeChangedAsync;
        }
    }
}
