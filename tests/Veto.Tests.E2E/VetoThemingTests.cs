using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for the theming engine, driven against /components/theming.</summary>
public class VetoThemingTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "components/theming";

    /// <summary>The <c>data-veto-theme</c> attribute on &lt;html&gt;, or null when following the OS.</summary>
    private Task<string?> ThemeAttributeAsync ()
        => Page.Locator("html").GetAttributeAsync("data-veto-theme");

    private Task<string> ResolvedColorAsync ( string token )
        => Page.EvaluateAsync<string>(
            $"() => getComputedStyle(document.documentElement).getPropertyValue('{token}').trim()");

    [Fact]
    public async Task Follows_the_operating_system_until_a_choice_is_made ()
    {
        await GotoComponentAsync(Route);

        Assert.Null(await ThemeAttributeAsync());
        await Expect(Page.GetByTestId("mode")).ToHaveTextAsync("System");
    }

    [Fact]
    public async Task Toggle_switches_to_dark_and_back ()
    {
        await GotoComponentAsync(Route);

        await Component("toggle").ClickAsync();
        Assert.Equal("dark", await ThemeAttributeAsync());
        await Expect(Page.GetByTestId("resolved")).ToHaveTextAsync("Dark");

        await Component("toggle").ClickAsync();
        Assert.Equal("light", await ThemeAttributeAsync());
        await Expect(Page.GetByTestId("resolved")).ToHaveTextAsync("Light");
    }

    [Fact]
    public async Task Toggle_reports_its_state_through_aria ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("toggle")).ToHaveAttributeAsync("role", "switch");
        await Expect(Component("toggle")).ToHaveAttributeAsync("aria-checked", "false");

        await Component("toggle").ClickAsync();
        await Expect(Component("toggle")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public async Task Segmented_control_selects_each_mode ()
    {
        await GotoComponentAsync(Route);

        var segmented = Component("segmented");
        await Expect(segmented).ToHaveAttributeAsync("role", "radiogroup");

        await segmented.Locator("[data-mode='Dark']").ClickAsync();
        Assert.Equal("dark", await ThemeAttributeAsync());
        await Expect(segmented.Locator("[data-mode='Dark']")).ToHaveAttributeAsync("aria-checked", "true");

        await segmented.Locator("[data-mode='Light']").ClickAsync();
        Assert.Equal("light", await ThemeAttributeAsync());

        await segmented.Locator("[data-mode='System']").ClickAsync();
        Assert.Null(await ThemeAttributeAsync());
        await Expect(Page.GetByTestId("mode")).ToHaveTextAsync("System");
    }

    [Fact]
    public async Task Both_toggles_stay_in_step_because_they_share_the_service ()
    {
        await GotoComponentAsync(Route);

        await Component("segmented").Locator("[data-mode='Dark']").ClickAsync();

        // The plain switch is a separate component instance reading the same service.
        await Expect(Component("toggle")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Component("layout-theme-toggle")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public async Task Switching_to_dark_actually_changes_the_token_values ()
    {
        await GotoComponentAsync(Route);

        var lightSurface = await ResolvedColorAsync("--veto-color-surface");
        var lightText = await ResolvedColorAsync("--veto-color-text");

        await Component("toggle").ClickAsync();

        Assert.NotEqual(lightSurface, await ResolvedColorAsync("--veto-color-surface"));
        Assert.NotEqual(lightText, await ResolvedColorAsync("--veto-color-text"));
    }

    [Fact]
    public async Task Choice_survives_a_reload_and_is_applied_before_first_paint ()
    {
        await GotoComponentAsync(Route);
        await Component("toggle").ClickAsync();
        Assert.Equal("dark", await ThemeAttributeAsync());

        await Page.ReloadAsync();

        // Read before waiting for interactivity: veto-theme-init.js must have applied the
        // attribute during the initial parse, otherwise the page flashes light on every load.
        Assert.Equal("dark", await ThemeAttributeAsync());

        await Page.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        await Expect(Page.GetByTestId("mode")).ToHaveTextAsync("Dark");
    }

    [Fact]
    public async Task Dark_mode_is_persisted_under_a_stable_storage_key ()
    {
        await GotoComponentAsync(Route);
        await Component("toggle").ClickAsync();

        var stored = await Page.EvaluateAsync<string?>("() => localStorage.getItem('veto-theme-mode')");
        Assert.Equal("Dark", stored);

        await Component("segmented").Locator("[data-mode='System']").ClickAsync();

        // System is the default, so it is stored as the absence of a value.
        Assert.Null(await Page.EvaluateAsync<string?>("() => localStorage.getItem('veto-theme-mode')"));
    }

    [Fact]
    public async Task System_mode_follows_the_browser_colour_scheme_preference ()
    {
        await GotoComponentAsync(Route);
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });

        await Expect(Page.GetByTestId("resolved")).ToHaveTextAsync("Dark");
        Assert.Null(await ThemeAttributeAsync());

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Expect(Page.GetByTestId("resolved")).ToHaveTextAsync("Light");
    }

    [Fact]
    public async Task Forced_mode_wins_over_the_operating_system_preference ()
    {
        await GotoComponentAsync(Route);
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });

        await Component("segmented").Locator("[data-mode='Light']").ClickAsync();

        Assert.Equal("light", await ThemeAttributeAsync());
        await Expect(Page.GetByTestId("resolved")).ToHaveTextAsync("Light");

        // The forced light palette must actually be in effect, not just the attribute. Compared
        // against the dark value rather than a hex literal: an app's own theme is free to
        // override the token, and asserting its exact text would break on a re-brand.
        var forcedLight = await ResolvedColorAsync("--veto-color-surface");

        await Component("segmented").Locator("[data-mode='Dark']").ClickAsync();
        var forcedDark = await ResolvedColorAsync("--veto-color-surface");

        Assert.NotEqual(forcedDark, forcedLight);
    }

    [Fact]
    public async Task Toggle_is_operable_from_the_keyboard ()
    {
        await GotoComponentAsync(Route);

        await Component("toggle").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Component("toggle")).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public async Task Token_swatches_are_rendered_for_the_documented_palette ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("swatches").Locator(".demo-swatch")).ToHaveCountAsync(8);
    }

    [Fact]
    public async Task A_choice_made_in_one_tab_is_followed_by_the_others ()
    {
        await GotoComponentAsync(Route);

        var other = await Context.NewPageAsync();
        await other.GotoAsync($"{App.BaseUrl}/{Route}");
        await other.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });

        await Component("toggle").ClickAsync();
        Assert.Equal("dark", await ThemeAttributeAsync());

        // The other tab hears the storage event: its document and its components follow.
        await Expect(other.Locator("html")).ToHaveAttributeAsync("data-veto-theme", "dark");
        await Expect(other.GetByTestId("resolved")).ToHaveTextAsync("Dark");
    }
}
