using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Components.Table;

/// <summary>
/// A templated table over a list of items: you supply the header cells, a row template and an
/// optional footer, and the table draws the rows, an empty state, stripes, hover, a sticky
/// header, and paging - Previous and Next, an infinite scroll, or virtualized rows.
/// </summary>
/// <remarks>
/// Rows come from <c>Items</c>, from <c>AsyncItems</c> - a stream read only as far as the
/// current page or scroll position needs - or from <c>ItemsProvider</c>, which fetches only the
/// rows in view. Sorting and selection are still outstanding - see <c>docs/Roadmap.md</c>.
/// </remarks>
/// <typeparam name="TItem">The type of the items being listed.</typeparam>
public partial class Table<TItem> : VetoDataComponentBase<TItem>
{
    private int _page;
    private int _pageSize;
    private int? _lastPageSizeParameter;
    private int _jumpVersion;
    private int? _lastPageIndexParameter;
    private int _shown;
    private bool _recheckSentinel;
    private ElementReference _sentinel;
    private ElementReference _scroller;
    private string? _observedSentinelId;
    private IJSObjectReference? _module;
    private IJSObjectReference? _observer;
    private DotNetObjectReference<Table<TItem>>? _self;
    private bool _rendered;
    private bool _scrollToTop;
    private Virtualize<TItem>? _virtualize;
    private ICollection<TItem>? _virtualItems;
    private IReadOnlyList<TItem>? _virtualItemsFrom;
    private ItemsProviderDelegate<TItem>? _lastProvider;
    private IEqualityComparer<TItem> _itemComparer = EqualityComparer<TItem>.Default;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <inheritdoc />
    protected override string RootClass => "veto-table";

    /// <summary>Content for the table's header row - usually a <c>&lt;th&gt;</c> per column.</summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Content for each row, given the item it represents - a <c>&lt;td&gt;</c> per column.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> RowTemplate { get; set; } = default!;

    /// <summary>Content for the table's footer row. No footer is rendered without it.</summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>What the body shows when there are no items, when it needs markup.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>What the body shows when there are no items. Ignored when <see cref="EmptyContent"/> is set.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "No data to show.";

    /// <summary>A caption naming the table, for sighted readers and assistive technology alike.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>Shades every other row.</summary>
    [Parameter]
    public bool Striped { get; set; } = true;

    /// <summary>Draws a border around every cell. Off leaves only the lines between rows.</summary>
    [Parameter]
    public bool Bordered { get; set; } = true;

    /// <summary>Highlights the row under the pointer.</summary>
    [Parameter]
    public bool Hover { get; set; } = true;

    /// <summary>Tighter cell padding, for data-heavy tables.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>
    /// Keeps the header in view while the rows scroll. Give the table a height to scroll within,
    /// with <c>RootMaxHeight</c>.
    /// </summary>
    [Parameter]
    public bool StickyHeader { get; set; }

    /// <summary>
    /// Raised when a row is clicked, or activated with Enter or Space. Setting it makes each row
    /// focusable.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> OnRowClick { get; set; }

    /// <summary>Extra classes for a row, worked out from its item - to flag an overdue invoice, say.</summary>
    [Parameter]
    public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>How the rows are split up: all at once, in pages, or as an infinite scroll.</summary>
    [Parameter]
    public VetoTablePaging Paging { get; set; } = VetoTablePaging.None;

    /// <summary>Rows per page, or per batch when scrolling. At least 1.</summary>
    [Parameter]
    public int PageSize { get; set; } = 25;

    /// <summary>The page shown, counting from 0, with <see cref="VetoTablePaging.Pages"/>. Bind it to follow or set the page.</summary>
    [Parameter]
    public int PageIndex { get; set; }

    /// <summary>Raised when Previous or Next moves to another page, or a new stream starts over at 0.</summary>
    [Parameter]
    public EventCallback<int> PageIndexChanged { get; set; }

    /// <summary>
    /// The page sizes offered in the pager's rows-per-page picker - 5, 10, 25, 50 and 100 unless
    /// you set your own, <c>PageSizeOptions="[20, 40, 80]"</c>. Null or empty hides the picker.
    /// The current <see cref="PageSize"/> is always offered, in the list or not.
    /// </summary>
    [Parameter]
    public IReadOnlyList<int>? PageSizeOptions { get; set; } = [5, 10, 25, 50, 100];

    /// <summary>Raised when the rows-per-page picker changes the page size. Bind PageSize to follow it.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>
    /// The range line in the pager, from the first and last row shown (counting from 1) and the
    /// total - null while a stream has not ended. Defaults to "11–20 of 1,000".
    /// </summary>
    [Parameter]
    public Func<int, int, int?, string>? RangeText { get; set; }

    /// <summary>
    /// The pager's status line, from the page number (counting from 1) and the page count - null
    /// while a stream has not ended and the count is not known yet.
    /// </summary>
    [Parameter]
    public Func<int, int?, string>? PageStatus { get; set; }

    /// <summary>The accessible name of the pager.</summary>
    [Parameter]
    public string PagerLabel { get; set; } = "Table pages";

    /// <summary>The First page button's accessible name and tooltip.</summary>
    [Parameter]
    public string FirstText { get; set; } = "First page";

    /// <summary>The Previous button's accessible name and tooltip.</summary>
    [Parameter]
    public string PreviousText { get; set; } = "Previous page";

    /// <summary>The Next button's accessible name and tooltip.</summary>
    [Parameter]
    public string NextText { get; set; } = "Next page";

    /// <summary>The Last page button's accessible name and tooltip.</summary>
    [Parameter]
    public string LastText { get; set; } = "Last page";

    /// <summary>The word before a page number, for the numbered buttons' accessible names: "Page 4".</summary>
    [Parameter]
    public string PageText { get; set; } = "Page";

    /// <summary>The rows-per-page picker's label.</summary>
    [Parameter]
    public string RowsPerPageText { get; set; } = "Rows per page";

    /// <summary>The go-to-page box's label.</summary>
    [Parameter]
    public string GoToPageText { get; set; } = "Go to page";

    /// <summary>The Load more button's text, with <see cref="VetoTablePaging.Scroll"/>.</summary>
    [Parameter]
    public string LoadMoreText { get; set; } = "Load more";

    /// <summary>Shown while rows are being read from <c>AsyncItems</c>.</summary>
    [Parameter]
    public string LoadingText { get; set; } = "Loading…";

    /// <summary>Shown when <c>AsyncItems</c> throws. The stream stops there.</summary>
    [Parameter]
    public string ErrorText { get; set; } = "The rows could not be loaded.";

    /// <summary>
    /// The height of one row in pixels, when virtualized. Virtualize uses it to size the space
    /// for the rows not rendered, so match it to the real row height - 37 for the default
    /// padding, about 29 with <see cref="Dense"/>.
    /// </summary>
    [Parameter]
    public float RowHeight { get; set; } = 37f;

    /// <summary>How many rows beyond the visible ones to render when virtualized, so a quick scroll does not show gaps.</summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>Whether only the rows in view are rendered: <see cref="VetoTablePaging.Virtualize"/>, or any ItemsProvider.</summary>
    private bool IsVirtual => ItemsProvider is not null || Paging == VetoTablePaging.Virtualize;

    /// <summary>
    /// Asks ItemsProvider again for the rows in view - after the data behind it changed, a new
    /// filter say. A new ItemsProvider delegate is picked up on its own.
    /// </summary>
    public Task RefreshAsync () => _virtualize?.RefreshDataAsync() ?? Task.CompletedTask;

    /// <inheritdoc />
    protected override int ItemsWanted => Paging switch
    {
        // One row past the page, so Next knows whether there is anything to go to.
        VetoTablePaging.Pages => SafeAdd(SafeMultiply(_page + 1, _pageSize), 1),
        VetoTablePaging.Scroll => _shown,
        _ => int.MaxValue
    };

    private int VisibleStart => Paging == VetoTablePaging.Pages ? Math.Min(SafeMultiply(_page, _pageSize), Rows.Count) : 0;

    private int VisibleCount => Paging switch
    {
        VetoTablePaging.Pages => Math.Min(_pageSize, Rows.Count - VisibleStart),
        VetoTablePaging.Scroll => Math.Min(_shown, Rows.Count),
        _ => Rows.Count
    };

    private bool HasPreviousPage => _page > 0;

    private bool HasNextPage => SafeMultiply(_page + 1, _pageSize) < Rows.Count;

    private int? PageCount => HasMoreItems ? null : Math.Max(1, (Rows.Count + _pageSize - 1) / _pageSize);

    private bool HasMoreRows => _shown < Rows.Count || HasMoreItems;

    /// <summary>The page sizes for the picker: the options, plus the current size if it is not one of them.</summary>
    private IEnumerable<int> PageSizeChoices =>
        (PageSizeOptions ?? []).Append(_pageSize).Where(size => size > 0).Distinct().Order();

    private bool ShowsPageSizePicker => PageSizeOptions is { Count: > 0 };

    private string RangeLine
    {
        get
        {
            var first = VisibleCount > 0 ? VisibleStart + 1 : 0;
            var last = VisibleStart + Math.Max(VisibleCount, 0);
            int? total = HasMoreItems ? null : Rows.Count;

            return RangeText is not null
                ? RangeText(first, last, total)
                : total is { } all ? $"{first:N0}–{last:N0} of {all:N0}" : $"{first:N0}–{last:N0}";
        }
    }

    /// <summary>
    /// The page numbers to offer, from 0, with null for a gap: the first and last page, and the
    /// pages either side of the current one - 1 … 4 5 6 … 100. A one-page gap shows the page
    /// instead of an ellipsis. While a stream has not ended, the pages read so far and a gap.
    /// </summary>
    private List<int?> PageWindow
    {
        get
        {
            var known = PageCount ?? Math.Max(1, (Rows.Count + _pageSize - 1) / _pageSize);
            var pages = new SortedSet<int> { 0, _page - 1, _page, _page + 1 };

            if (PageCount is not null)
            {
                pages.Add(known - 1);
            }

            List<int?> window = [];
            int? previous = null;

            foreach (var page in pages.Where(page => page >= 0 && page < known))
            {
                if (previous is { } before && page - before == 2)
                {
                    window.Add(before + 1);
                }
                else if (previous is { } gapStart && page - gapStart > 2)
                {
                    window.Add(null);
                }

                window.Add(page);
                previous = page;
            }

            if (PageCount is null)
            {
                window.Add(null);
            }

            return window;
        }
    }

    /// <summary>The rows for Virtualize when it is given Items: the sorted rows, as the ICollection it takes.</summary>
    private ICollection<TItem>? VirtualItems
    {
        get
        {
            if (!IsVirtual || ItemsProvider is not null)
            {
                return null;
            }

            if (!ReferenceEquals(_virtualItemsFrom, Rows))
            {
                _virtualItemsFrom = Rows;
                _virtualItems = Rows as ICollection<TItem> ?? [.. Rows];
            }

            return _virtualItems;
        }
    }

    private string PageStatusText => PageStatus is not null
        ? PageStatus(_page + 1, PageCount)
        : PageCount is { } count ? $"Page {_page + 1} of {count}" : $"Page {_page + 1}";

    private string? Part ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        if (PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "PageSize must be at least 1.");
        }

        // Like PageIndex: adopted when the parent changes it, so the picker's choice survives a
        // re-render that passes the old value again.
        if (PageSize != _lastPageSizeParameter)
        {
            _lastPageSizeParameter = PageSize;
            _pageSize = PageSize;
        }

        AdoptSortParameter();

        // Adopt PageIndex only when the parent actually changes it, so a re-render that passes
        // the same value does not undo a page the user moved to.
        if (PageIndex != _lastPageIndexParameter)
        {
            _lastPageIndexParameter = PageIndex;
            _page = Math.Max(0, PageIndex);
        }

        if (_shown == 0)
        {
            _shown = _pageSize;
        }

        base.OnParametersSet();

        if (IsVirtual && AsyncItems is not null)
        {
            throw new InvalidOperationException(
                "A virtualized Table needs Items or ItemsProvider; AsyncItems can only be read in order. Use Paging=\"Scroll\" for a stream.");
        }

        // Virtualize recognises the same row across fetches by this; ItemKey says what that is.
        _itemComparer = ItemKey is null ? EqualityComparer<TItem>.Default : new KeyComparer(ItemKey);

        // A shorter list can leave the page past its end, show the last page instead.
        if (Paging == VetoTablePaging.Pages && !HasMoreItems && _page > 0 && SafeMultiply(_page, _pageSize) >= Rows.Count)
        {
            _page = Math.Max(0, (Rows.Count - 1) / _pageSize);
        }
    }

    /// <inheritdoc />
    protected override void OnItemsSourceReset ()
    {
        _shown = _pageSize;
        _recheckSentinel = true;

        // Starting over means starting at the top - but not on the very first source, which
        // would scroll a page that has only just loaded.
        _scrollToTop = _rendered;

        if (_page != 0)
        {
            _page = 0;
            _ = PageIndexChanged.InvokeAsync(0);
        }
    }

    /// <summary>
    /// Moves to a page, counting from 0 - the one place every pager control goes through. With a
    /// stream it reads up to that page first (and one row past it, so Next knows whether to stay
    /// enabled); a page past the end lands on the last one.
    /// </summary>
    private async Task GoToPageAsync ( int target )
    {
        if (IsLoadingItems)
        {
            return;
        }

        target = Math.Max(0, target);

        if (HasMoreItems)
        {
            await LoadItemsAsync(SafeAdd(SafeMultiply(target + 1, _pageSize), 1));
        }

        target = Math.Min(target, Math.Max(0, (Rows.Count - 1) / _pageSize));

        // A new key on the go-to box either way: one still holding an out-of-range number must
        // show the real page again, and Blazor would not patch a value it thinks is unchanged.
        _jumpVersion++;

        if (target == _page)
        {
            return;
        }

        _page = target;
        _scrollToTop = true;
        await PageIndexChanged.InvokeAsync(_page);
    }

    private Task JumpAsync ( ChangeEventArgs e ) =>
        int.TryParse(e.Value?.ToString(), out var number) ? GoToPageAsync(number - 1) : Task.CompletedTask;

    /// <summary>A new page size keeps the first row in view: row 21 at 10 a page stays on screen at 25.</summary>
    private async Task ChangePageSizeAsync ( ChangeEventArgs e )
    {
        if (!int.TryParse(e.Value?.ToString(), out var size) || size < 1 || size == _pageSize)
        {
            return;
        }

        var firstRow = SafeMultiply(_page, _pageSize);
        _pageSize = size;
        _shown = size;
        var page = firstRow / size;

        await PageSizeChanged.InvokeAsync(size);

        if (page != _page)
        {
            _page = page;
            await PageIndexChanged.InvokeAsync(page);
        }

        if (HasMoreItems)
        {
            await LoadItemsAsync(ItemsWanted);
        }

        _scrollToTop = true;
    }

    private async Task LoadMoreAsync ()
    {
        if (!HasMoreRows || IsLoadingItems)
        {
            return;
        }

        _shown = SafeAdd(_shown, _pageSize);

        // Set before the read, so the render that ends it - once loading is over - asks the
        // observer to look again in case the new rows did not fill the view.
        _recheckSentinel = true;
        await LoadItemsAsync(_shown);

        // Needed despite the analyzer: called from JavaScript, nothing re-renders afterwards on
        // its own - and with Items nothing was read, so nothing else has rendered the new rows.
#pragma warning disable BL0012
        StateHasChanged();
#pragma warning restore BL0012
    }

    /// <summary>Called by the scroll observer when the end of the table comes into view.</summary>
    [JSInvokable]
    public Task OnSentinelVisibleAsync () => LoadMoreAsync();

    /// <summary>Whether the markup rendered the sentinel - the same test Table.razor uses.</summary>
    private bool SentinelRendered =>
        Paging == VetoTablePaging.Scroll && VisibleCount > 0 && HasMoreRows && ItemsError is null;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        _rendered = true;

        try
        {
            if (_scrollToTop)
            {
                // A new page or a new stream: show its first row, not wherever the last one was
                // scrolled to. Only the table's own scroll box - the page stays where it is, so
                // Next stays under the pointer.
                _scrollToTop = false;
                _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Table/veto-table.js");
                await _module.InvokeVoidAsync("scrollToTop", _scroller);
            }

            // Delegate equality, not reference: ItemsProvider="Load" makes a new delegate object
            // for the same method on every render, and that must not refetch each time.
            if (IsVirtual && ItemsProvider != _lastProvider)
            {
                var replaced = _lastProvider is not null;
                _lastProvider = ItemsProvider;

                if (replaced && _virtualize is not null)
                {
                    await _virtualize.RefreshDataAsync();
                }
            }

            // A sentinel that left the DOM and came back - a new stream empties the table while
            // it loads - is a new element. The old observer is watching nothing; replace it.
            if (_observer is not null && (!SentinelRendered || _sentinel.Id != _observedSentinelId))
            {
                await DisposeObserverAsync();
            }

            if (SentinelRendered && _observer is null)
            {
                _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Table/veto-table.js");
                _self ??= DotNetObjectReference.Create(this);
                _observer = await _module.InvokeAsync<IJSObjectReference>("observeEnd", _sentinel, _self);
                _observedSentinelId = _sentinel.Id;

                // Observing afresh reports the sentinel's state at once, so no recheck is due.
                _recheckSentinel = false;
            }
            else if (_recheckSentinel && !IsLoadingItems && _observer is not null)
            {
                // The observer only reports changes. If a batch did not fill the view, the end is
                // still visible and nothing would fire again - so ask it to look once more.
                _recheckSentinel = false;
                await _observer.InvokeVoidAsync("recheck");
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // No script, or the page is going away. The Load more button still works.
        }
    }

    private async Task DisposeObserverAsync ()
    {
        if (_observer is null)
        {
            return;
        }

        var observer = _observer;
        _observer = null;
        _observedSentinelId = null;

        try
        {
            await observer.InvokeVoidAsync("dispose");
            await observer.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // The page is already gone.
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync ()
    {
        await DisposeObserverAsync();

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The page is already gone.
            }
        }

        _self?.Dispose();
        await base.DisposeAsync();
    }

    /// <summary>Two items are the same row when ItemKey says so.</summary>
    private sealed class KeyComparer ( Func<TItem, object?> key ) : IEqualityComparer<TItem>
    {
        public bool Equals ( TItem? x, TItem? y ) =>
            x is null || y is null ? x is null && y is null : Object.Equals(key(x), key(y));

        public int GetHashCode ( TItem item ) => key(item)?.GetHashCode() ?? 0;
    }

    private static int SafeMultiply ( int a, int b ) => (int)Math.Min((long)a * b, int.MaxValue);

    private static int SafeAdd ( int a, int b ) => a > int.MaxValue - b ? int.MaxValue : a + b;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("striped"), Striped && !IsVirtual)
            .AddClass(Modifier("virtual"), IsVirtual)
            .AddClass(Modifier("bordered"), Bordered)
            .AddClass(Modifier("hover"), Hover)
            .AddClass(Modifier("dense"), Dense)
            .AddClass(Modifier("sticky"), StickyHeader)
            .AddClass(Modifier("clickable"), OnRowClick.HasDelegate);

    private string RowCss ( TItem item )
    {
        var extra = RowClass?.Invoke(item);
        return string.IsNullOrWhiteSpace(extra) ? "veto-table__row" : $"veto-table__row {extra}";
    }

    private Task OnRowKeyDownAsync ( KeyboardEventArgs e, TItem item ) =>
        e.Key is "Enter" or " " ? OnRowClick.InvokeAsync(item) : Task.CompletedTask;
}