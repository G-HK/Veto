using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>Table&lt;TItem&gt;</c>, driven against /docs/components/table.</summary>
public class VetoTableTests ( BlazorAppFixture app ) : VetoPageTest(app)
{
    private const string Route = "docs/components/table";

    [Fact]
    public async Task Renders_a_row_per_item_with_header_caption_and_footer ()
    {
        await GotoComponentAsync(Route);

        var table = Component("basic");
        await Expect(table).ToHaveClassAsync(new Regex(@"\bveto-table\b"));
        await Expect(table.Locator("caption")).ToHaveTextAsync("Recent orders");
        await Expect(table.Locator("thead th")).ToHaveCountAsync(3);
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(3);
        await Expect(table.Locator("tbody tr").First).ToContainTextAsync("Ada Lovelace");
        await Expect(Page.GetByTestId("basic-total")).ToHaveTextAsync("181.25");
    }

    [Fact]
    public async Task A_null_list_shows_the_empty_text_instead_of_failing ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("empty-empty")).ToHaveTextAsync("No data to show.");
        await Expect(Component("empty").Locator("tfoot")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Empty_content_replaces_the_empty_text ()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("empty-custom-empty").Locator("strong")).ToHaveTextAsync("No orders yet.");
    }

    [Fact]
    public async Task Look_options_become_modifier_classes ()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveClassAsync(new Regex(@"\bveto-table--striped\b.*\bveto-table--bordered\b.*\bveto-table--hover\b"));

        var plain = Component("plain");
        await Expect(plain).ToHaveClassAsync(new Regex(@"\bveto-table--dense\b"));
        await Expect(plain).Not.ToHaveClassAsync(new Regex(@"\bveto-table--striped\b"));
        await Expect(plain).Not.ToHaveClassAsync(new Regex(@"\bveto-table--bordered\b"));
    }

    [Fact]
    public async Task Clicking_or_pressing_enter_on_a_row_raises_its_item ()
    {
        await GotoComponentAsync(Route);

        var rows = Component("clickable").Locator("tbody tr");
        await rows.Nth(2).ClickAsync();
        await Expect(Page.GetByTestId("clickable-value")).ToHaveTextAsync("#2003");

        await rows.Nth(4).FocusAsync();
        await rows.Nth(4).PressAsync("Enter");
        await Expect(Page.GetByTestId("clickable-value")).ToHaveTextAsync("#2005");
    }

    [Fact]
    public async Task Row_class_is_worked_out_from_the_item ()
    {
        await GotoComponentAsync(Route);

        var rows = Component("clickable").Locator("tbody tr");

        // Total is i * 11.5, so the ninth order (103.5) is the first over 100.
        await Expect(rows.Nth(7)).Not.ToHaveClassAsync(new Regex(@"\bdemo-large-order\b"));
        await Expect(rows.Nth(8)).ToHaveClassAsync(new Regex(@"\bdemo-large-order\b"));
    }

    [Fact]
    public async Task Sticky_header_stays_in_view_while_rows_scroll ()
    {
        await GotoComponentAsync(Route);

        var table = Component("clickable");
        var header = table.Locator("thead th").First;
        var before = await header.EvaluateAsync<double>("el => el.getBoundingClientRect().top");

        await table.EvaluateAsync("el => el.scrollTop = 200");
        await Expect(table).ToHaveJSPropertyAsync("scrollTop", 200);

        var after = await header.EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        Assert.Equal(before, after, 1);
    }
}
