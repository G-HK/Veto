using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace Veto.Tests.E2E.Infrastructure;

/// <summary>
/// Base class for every Veto component test. Adds demo-app navigation on top of
/// Playwright's <see cref="PageTest"/>, which already supplies a fresh browser
/// context and <see cref="PageTest.Page"/> per test.
/// </summary>
[Collection( DemoAppCollection.Name )]
public abstract class VetoPageTest ( BlazorAppFixture app ) : PageTest
{
    /// <summary>The running demo app.</summary>
    protected BlazorAppFixture App { get; } = app;

    /// <summary>
    /// Navigates to a demo page and waits until Blazor WebAssembly has taken over from
    /// the prerendered markup. Pages mark this by setting <c>data-ready="true"</c> on
    /// their root after the first interactive render - waiting on network idle is not
    /// enough, because the prerendered HTML is present and inert long before then.
    /// </summary>
    protected async Task GotoComponentAsync ( string route )
    {
        await Page.GotoAsync($"{App.BaseUrl}/{route.TrimStart('/')}");
        await Page.Locator("[data-testid='page'][data-ready='true']")
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
    }

    /// <summary>The root element of the component with the given <c>TestId</c>.</summary>
    protected ILocator Component ( string testId ) => Page.GetByTestId(testId);

    /// <summary>The inner input element of the component with the given <c>TestId</c>.</summary>
    protected ILocator Input ( string testId ) => Component(testId).Locator("input");
}
