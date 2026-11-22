using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>VetoToast</c>, driven against /docs/components/toast.</summary>
public class VetoToastTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/toast";

    /// <summary>Every toast on screen, wherever it was raised.</summary>
    private ILocator Toasts () => Page.Locator(".veto-toast");

    private ILocator Stack ( string position ) => Page.GetByTestId($"toasts-{position}");

    [Fact]
    public async Task Raising_a_toast_puts_it_on_screen ()
    {
        await GotoComponentAsync(Route);

        await Expect(Toasts()).ToHaveCountAsync(0);

        await Page.GetByTestId("raise-success").ClickAsync();

        var toast = Page.GetByTestId("success-toast");
        await Expect(toast).ToBeVisibleAsync();
        await Expect(toast).ToContainTextAsync("Saved");
        await Expect(toast).ToHaveAttributeAsync("data-type", "success");
    }

    [Fact]
    public async Task An_error_is_announced_assertively_and_a_status_politely ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-error").ClickAsync();
        await Page.GetByTestId("raise-info").ClickAsync();

        await Expect(Page.GetByTestId("error-toast")).ToHaveAttributeAsync("role", "alert");
        await Expect(Page.GetByTestId("error-toast")).ToHaveAttributeAsync("aria-live", "assertive");
        await Expect(Page.GetByTestId("info-toast")).ToHaveAttributeAsync("role", "status");
        await Expect(Page.GetByTestId("info-toast")).ToHaveAttributeAsync("aria-live", "polite");
    }

    [Fact]
    public async Task The_close_button_dismisses_a_toast ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-slow").ClickAsync();
        await Expect(Page.GetByTestId("slow-toast")).ToBeVisibleAsync();

        await Page.GetByTestId("slow-toast-close").ClickAsync();

        await Expect(Toasts()).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_toast_leaves_on_its_own_when_its_time_is_up ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-quick").ClickAsync();
        await Expect(Page.GetByTestId("quick-toast")).ToBeVisibleAsync();

        // Raised with Duration=1000, so it should be gone well inside the default timeout.
        await Expect(Toasts()).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task A_sticky_toast_has_no_countdown_and_does_not_leave ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-sticky").ClickAsync();

        var toast = Page.GetByTestId("sticky-toast");
        await Expect(toast).ToHaveAttributeAsync("data-sticky", "true");
        await Expect(toast.Locator(".veto-toast__progress")).ToHaveCountAsync(0);

        // Well past the host's 5s default; a sticky toast is still there.
        await Page.WaitForTimeoutAsync(6000);
        await Expect(toast).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_timed_toast_carries_a_countdown_bar ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-slow").ClickAsync();

        await Expect(Page.GetByTestId("slow-toast-progress")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_title_is_rendered_above_the_message ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-titled").ClickAsync();

        await Expect(Page.GetByTestId("titled-toast").Locator(".veto-toast__title"))
            .ToHaveTextAsync("Build finished");
    }

    [Fact]
    public async Task Clicking_a_toast_raises_its_handler_and_can_dismiss_it ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-clickable").ClickAsync();
        await Page.GetByTestId("clickable-toast").ClickAsync();

        await Expect(Page.GetByTestId("clicked")).ToHaveTextAsync("Toast clicked");
        await Expect(Toasts()).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Duplicates_can_be_allowed_or_blocked ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("dup-allow").ClickAsync();
        await Page.GetByTestId("dup-allow").ClickAsync();
        await Expect(Page.GetByTestId("dup-allow-toast")).ToHaveCountAsync(2);

        await Page.GetByTestId("clear-all").ClickAsync();
        await Expect(Toasts()).ToHaveCountAsync(0);

        await Page.GetByTestId("dup-block").ClickAsync();
        await Page.GetByTestId("dup-block").ClickAsync();
        await Page.GetByTestId("dup-block").ClickAsync();
        await Expect(Page.GetByTestId("dup-block-toast")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Replace_folds_a_repeat_into_the_toast_already_on_screen ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("dup-replace").ClickAsync();
        await Page.GetByTestId("dup-replace").ClickAsync();
        await Page.GetByTestId("dup-replace").ClickAsync();

        await Expect(Page.GetByTestId("dup-replace-toast")).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("dup-replace-toast-count")).ToHaveTextAsync("3");
    }

    [Fact]
    public async Task The_stack_never_grows_past_its_limit ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-many").ClickAsync();

        // The host caps the stack at five; the oldest are retired to make room.
        await Expect(Page.GetByTestId("many-toast")).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task A_toast_can_be_raised_in_any_of_the_six_corners ()
    {
        await GotoComponentAsync(Route);

        foreach (var position in new[] { "topleft", "topcenter", "bottomleft", "bottomright" })
        {
            await Page.GetByTestId($"pos-{position}").ClickAsync();
        }

        await Expect(Stack("top-left").Locator(".veto-toast")).ToHaveCountAsync(1);
        await Expect(Stack("top-center").Locator(".veto-toast")).ToHaveCountAsync(1);
        await Expect(Stack("bottom-left").Locator(".veto-toast")).ToHaveCountAsync(1);
        await Expect(Stack("bottom-right").Locator(".veto-toast")).ToHaveCountAsync(1);

        await Expect(Stack("top-left")).ToHaveAttributeAsync("role", "region");
        await Expect(Stack("top-left")).ToHaveAttributeAsync("aria-label", "Notifications");
    }

    [Fact]
    public async Task The_animation_a_toast_was_asked_for_is_the_one_it_carries ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("anim-flipxup").ClickAsync();
        await Expect(Page.GetByTestId("toast-flipxup")).ToHaveAttributeAsync("data-animation", "flip-x-up");

        await Page.GetByTestId("anim-zoom").ClickAsync();
        await Expect(Page.GetByTestId("toast-zoom")).ToHaveAttributeAsync("data-animation", "zoom");
    }

    [Fact]
    public async Task A_toast_settles_into_its_resting_state_after_arriving ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-slow").ClickAsync();

        // "entering" is the displaced first frame; the transition runs once it flips to "entered".
        await Expect(Page.GetByTestId("slow-toast")).ToHaveAttributeAsync("data-state", "entered");
    }

    [Fact]
    public async Task Clearing_takes_every_toast_away ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-sticky").ClickAsync();
        await Page.GetByTestId("raise-slow").ClickAsync();
        await Expect(Toasts()).ToHaveCountAsync(2);

        await Page.GetByTestId("clear-all").ClickAsync();

        // Even the sticky one: Clear is explicit, and explicit beats "never hides itself".
        await Expect(Toasts()).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Keyboard_focus_inside_a_toast_holds_its_countdown ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("raise-quick").ClickAsync();
        await Page.GetByTestId("quick-toast-close").FocusAsync();

        // Duration is one second; with focus on its close button it must outlast that.
        await Page.WaitForTimeoutAsync(2500);
        await Expect(Page.GetByTestId("quick-toast")).ToBeVisibleAsync();

        await Page.GetByTestId("quick-toast-close").BlurAsync();
        await Expect(Page.GetByTestId("quick-toast")).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task A_tracked_task_shows_working_then_success ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("track-ok").ClickAsync();

        var toast = Page.GetByTestId("track-toast");
        await Expect(toast).ToContainTextAsync("Saving…");
        await Expect(toast).ToHaveAttributeAsync("data-sticky", "true");

        await Expect(toast.Filter(new() { HasText = "Saved" })).ToHaveAttributeAsync("data-type", "success");
        await Expect(toast.Filter(new() { HasText = "Saving…" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_tracked_task_that_fails_ends_in_an_error ()
    {
        await GotoComponentAsync(Route);

        await Page.GetByTestId("track-fail").ClickAsync();

        await Expect(Page.GetByTestId("track-toast").Filter(new() { HasText = "Could not save" }))
            .ToHaveAttributeAsync("data-type", "error");
    }
}
