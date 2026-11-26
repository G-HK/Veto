using Microsoft.Playwright;
using Veto.Tests.E2E.Infrastructure;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>End-to-end specs for <c>Table&lt;TItem&gt;</c>, driven against /docs/components/table.</summary>
public class VetoTableTests(BlazorAppFixture app) : VetoPageTest(app)
{
    private const string Route = "docs/components/table";

    [Fact]
    public async Task Renders_a_row_per_item_with_header_caption_and_footer()
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
    public async Task A_null_list_shows_the_empty_text_instead_of_failing()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("empty-empty")).ToHaveTextAsync("No data to show.");
        await Expect(Component("empty").Locator("tfoot")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Empty_content_replaces_the_empty_text()
    {
        await GotoComponentAsync(Route);

        await Expect(Page.GetByTestId("empty-custom-empty").Locator("strong")).ToHaveTextAsync("No orders yet.");
    }

    [Fact]
    public async Task Look_options_become_modifier_classes()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("basic")).ToHaveClassAsync(new Regex(@"\bveto-table--striped\b.*\bveto-table--bordered\b.*\bveto-table--hover\b"));

        var plain = Component("plain");
        await Expect(plain).ToHaveClassAsync(new Regex(@"\bveto-table--dense\b"));
        await Expect(plain).Not.ToHaveClassAsync(new Regex(@"\bveto-table--striped\b"));
        await Expect(plain).Not.ToHaveClassAsync(new Regex(@"\bveto-table--bordered\b"));
    }

    [Fact]
    public async Task Clicking_or_pressing_enter_on_a_row_raises_its_item()
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
    public async Task Row_class_is_worked_out_from_the_item()
    {
        await GotoComponentAsync(Route);

        var rows = Component("clickable").Locator("tbody tr");

        // Total is i * 11.5, so the ninth order (103.5) is the first over 100.
        await Expect(rows.Nth(7)).Not.ToHaveClassAsync(new Regex(@"\bdemo-large-order\b"));
        await Expect(rows.Nth(8)).ToHaveClassAsync(new Regex(@"\bdemo-large-order\b"));
    }

    [Fact]
    public async Task Sticky_header_stays_in_view_while_rows_scroll()
    {
        await GotoComponentAsync(Route);

        var table = Component("clickable");
        var scroller = Component("clickable-scroll");
        var header = table.Locator("thead th").First;
        var before = await header.EvaluateAsync<double>("el => el.getBoundingClientRect().top");

        await scroller.EvaluateAsync("el => el.scrollTop = 200");
        await Expect(scroller).ToHaveJSPropertyAsync("scrollTop", 200);

        var after = await header.EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        Assert.Equal(before, after, 1);
    }

    [Fact]
    public async Task Pages_show_one_page_at_a_time_with_the_count_known_up_front()
    {
        await GotoComponentAsync(Route);

        var table = Component("paged");
        var scroller = Component("paged-scroll");
        var rows = table.Locator("tbody tr");
        var previous = Component("paged-previous");
        var next = Component("paged-next");

        // All 1,000 demo cars, ten to a page.
        await Expect(rows).ToHaveCountAsync(10);
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 100");
        await Expect(previous).ToBeDisabledAsync();
        await Expect(rows.First).ToHaveAttributeAsync("data-index", "0");

        // Scrolled down inside RootMaxHeight, a new page still starts at its first row.
        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await next.ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 2 of 100");
        await Expect(rows.First).ToHaveAttributeAsync("data-index", "10");
        await Expect(scroller).ToHaveJSPropertyAsync("scrollTop", 0);
        await Expect(previous).ToBeEnabledAsync();

        await previous.ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 100");
    }

    [Fact]
    public async Task A_bound_page_index_moves_the_table_and_follows_it()
    {
        await GotoComponentAsync(Route);

        // Setting PageIndex from outside moves the table...
        await Component("paged-set-last").ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 100 of 100");
        await Expect(Component("paged-next")).ToBeDisabledAsync();
        await Expect(Component("paged").Locator("tbody tr").First).ToHaveAttributeAsync("data-index", "990");

        // ...and Previous inside the table is reported back out.
        await Component("paged-previous").ClickAsync();
        await Expect(Component("paged-index")).ToHaveTextAsync("98");

        await Component("paged-set-first").ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 100");
    }

    [Fact]
    public async Task A_stream_is_read_a_page_at_a_time_and_the_count_appears_when_it_ends()
    {
        await GotoComponentAsync(Route);

        // One dealer's stock, five to a page: 83 cars, so 17 pages, the last holding three.
        var rows = Component("stream-paged").Locator("tbody tr");
        var next = Component("stream-paged-next");

        await Expect(rows).ToHaveCountAsync(5);
        // The stream has not ended, so the count is not known yet.
        await Expect(Component("stream-paged-page")).ToHaveTextAsync("Page 1");

        for (var page = 2; page <= 16; page++)
        {
            await next.ClickAsync();
            await Expect(Component("stream-paged-page")).ToHaveTextAsync($"Page {page}");
            await Expect(rows.First).ToHaveAttributeAsync("data-index", $"{(page - 1) * 5}");
        }

        // Reading page 17 runs the stream dry: now the count is known, and there is no Next.
        await next.ClickAsync();
        await Expect(Component("stream-paged-page")).ToHaveTextAsync("Page 17 of 17");
        await Expect(next).ToBeDisabledAsync();
        await Expect(rows).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task Scrolling_to_the_end_reads_the_next_batch()
    {
        await GotoComponentAsync(Route);

        var table = Component("feed");
        var scroller = Component("feed-scroll");
        var rows = table.Locator("tbody tr");

        // The observer watches the viewport: the table has to be on screen before scrolling
        // inside it can bring its end into view.
        await table.ScrollIntoViewIfNeededAsync();
        await Expect(rows).ToHaveCountAsync(10);

        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(rows).ToHaveCountAsync(20);

        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(rows).ToHaveCountAsync(30);
    }

    [Fact]
    public async Task Load_more_reads_the_next_batch_from_the_keyboard()
    {
        await GotoComponentAsync(Route);

        var rows = Component("feed").Locator("tbody tr");
        await Expect(rows).ToHaveCountAsync(10);

        await Component("feed-more").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(rows).ToHaveCountAsync(20);
    }

    [Fact]
    public async Task A_new_stream_starts_the_feed_over_from_the_top()
    {
        await GotoComponentAsync(Route);

        var table = Component("feed");
        var scroller = Component("feed-scroll");
        var rows = table.Locator("tbody tr");

        await table.ScrollIntoViewIfNeededAsync();
        // The first batch has to be there before there is an end to scroll to.
        await Expect(rows).ToHaveCountAsync(10);
        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(rows).ToHaveCountAsync(20);

        await Component("feed-electric").ClickAsync();

        // A new stream: back to the top, one batch, every row from it.
        await Expect(rows).ToHaveCountAsync(10);
        await Expect(scroller).ToHaveJSPropertyAsync("scrollTop", 0);
        foreach (var fuel in await table.Locator("tbody tr td:nth-child(2)").AllTextContentsAsync())
        {
            Assert.Equal("Electric", fuel.Trim());
        }

        // 162 electric cars in the demo data. Scroll to the end until the feed runs out.
        for (var batch = 2; batch <= 17; batch++)
        {
            await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
            await Expect(rows).ToHaveCountAsync(Math.Min(batch * 10, 162));
        }

        await Expect(Component("feed-more")).ToHaveCountAsync(0);
        await Expect(Component("feed-sentinel")).ToHaveCountAsync(0);
        Assert.All(await table.Locator("tbody tr td:nth-child(2)").AllTextContentsAsync(),
            fuel => Assert.Equal("Electric", fuel.Trim()));
    }

    [Fact]
    public async Task A_virtualized_table_renders_only_the_rows_in_view()
    {
        await GotoComponentAsync(Route);

        var table = Component("virtual");
        var scroller = Component("virtual-scroll");
        var rows = table.Locator("tbody tr.veto-table__row");

        await table.ScrollIntoViewIfNeededAsync();
        await Expect(table.GetByRole(AriaRole.Cell, new() { Name = "1", Exact = true })).ToBeVisibleAsync();

        // A thousand cars, a screenful of rows in the page.
        var rendered = await rows.CountAsync();
        Assert.InRange(rendered, 5, 40);

        // Alternate shading would flicker as rows come and go, so it is off.
        await Expect(table).ToHaveClassAsync(new Regex(@"\bveto-table--virtual\b"));
        await Expect(table).Not.ToHaveClassAsync(new Regex(@"\bveto-table--striped\b"));

        // The last row is reachable by scrolling, fetched when it comes into view.
        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(table.GetByRole(AriaRole.Cell, new() { Name = "1000", Exact = true })).ToBeVisibleAsync();
        Assert.InRange(await rows.CountAsync(), 5, 40);
    }

    [Fact]
    public async Task The_pager_offers_page_numbers_around_the_current_page()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("paged-range")).ToHaveTextAsync("1–10 of 1,000");
        await Expect(Component("paged-first")).ToBeDisabledAsync();
        await Expect(Component("paged-page-1")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Component("paged-page-100")).ToBeVisibleAsync();

        await Component("paged-page-100").ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 100 of 100");
        await Expect(Component("paged-range")).ToHaveTextAsync("991–1,000 of 1,000");
        await Expect(Component("paged-next")).ToBeDisabledAsync();
        await Expect(Component("paged-last")).ToBeDisabledAsync();

        // The pages either side of the current one, plus the first and the last.
        await Component("paged-jump").FillAsync("50");
        await Component("paged-jump").PressAsync("Enter");
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 50 of 100");
        foreach (var page in new[] { 1, 49, 50, 51, 100 })
        {
            await Expect(Component($"paged-page-{page}")).ToBeVisibleAsync();
        }
        await Expect(Component("paged-page-48")).ToHaveCountAsync(0);

        await Component("paged-first").ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 100");
    }

    [Fact]
    public async Task Going_to_a_page_past_the_end_lands_on_the_last_one()
    {
        await GotoComponentAsync(Route);

        var jump = Component("paged-jump");
        await jump.FillAsync("500");
        await jump.PressAsync("Enter");

        await Expect(Component("paged-page")).ToHaveTextAsync("Page 100 of 100");
        // The box shows the page actually reached, not the number typed.
        await Expect(jump).ToHaveValueAsync("100");
    }

    [Fact]
    public async Task Rows_per_page_keeps_the_first_row_in_view()
    {
        await GotoComponentAsync(Route);

        var picker = Component("paged-page-size");
        await Expect(picker.Locator("option")).ToHaveTextAsync(["5", "10", "25", "50", "100"]);
        await Expect(picker).ToHaveValueAsync("10");

        // Rows 21-30 on page 3; at 25 a page, row 21 is on page 1.
        await Component("paged-jump").FillAsync("3");
        await Component("paged-jump").PressAsync("Enter");
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 3 of 100");
        await picker.SelectOptionAsync("25");

        await Expect(Component("paged").Locator("tbody tr")).ToHaveCountAsync(25);
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 40");
        await Expect(Component("paged-range")).ToHaveTextAsync("1–25 of 1,000");
    }

    [Fact]
    public async Task A_developer_can_offer_their_own_page_sizes()
    {
        await GotoComponentAsync(Route);

        await Expect(Component("stream-paged-page-size").Locator("option")).ToHaveTextAsync(["5", "15", "30"]);
        await Expect(Component("stream-paged").Locator("tbody tr")).ToHaveCountAsync(5);

        // Last needs the count, which a stream only has once it ends.
        await Expect(Component("stream-paged-last")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task A_sortable_header_sorts_ascending_then_descending_then_not_at_all()
    {
        await GotoComponentAsync(Route);

        var header = Component("paged-sort-price");
        var button = Component("paged-sort-price-button");
        var prices = Component("paged").Locator("tbody tr td:nth-child(4)");

        await Component("paged-next").ClickAsync();
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 2 of 100");

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        // A new order starts at the first page.
        await Expect(Component("paged-page")).ToHaveTextAsync("Page 1 of 100");
        var ascending = Numbers(await prices.AllTextContentsAsync());
        Assert.Equal(ascending.Order(), ascending);

        await button.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "descending");
        var descending = Numbers(await prices.AllTextContentsAsync());
        Assert.Equal(descending.OrderDescending(), descending);
        Assert.True(descending[0] > ascending[^1]);

        await button.ClickAsync();
        await Expect(header).Not.ToHaveAttributeAsync("aria-sort", new Regex("."));
    }

    [Fact]
    public async Task A_provider_sorts_by_the_bound_sort_when_a_header_is_clicked()
    {
        await GotoComponentAsync(Route);

        var table = Component("virtual");
        await table.ScrollIntoViewIfNeededAsync();
        await Expect(table.GetByRole(AriaRole.Cell, new() { Name = "1", Exact = true })).ToBeVisibleAsync();

        await Component("virtual-sort-power-button").ClickAsync();
        await Expect(Component("virtual-sort-power")).ToHaveAttributeAsync("aria-sort", "ascending");

        // The provider read the new sort and the table asked it again: car 1 is no longer first.
        var powers = table.Locator("tbody tr.veto-table__row td:nth-child(4)");
        await Expect(table.Locator("tbody tr.veto-table__row td:first-child").First).Not.ToHaveTextAsync("1");
        var shown = Numbers(await powers.AllTextContentsAsync());
        Assert.Equal(shown.Order(), shown);
    }

    private static List<decimal> Numbers ( IEnumerable<string> texts ) =>
        [.. texts.Select(text => decimal.Parse(new string([.. text.Where(char.IsDigit)])))];
}
