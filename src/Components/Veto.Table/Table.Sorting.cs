using Microsoft.AspNetCore.Components;

namespace Veto.Components.Table;

// Sorting: the TableSortHeader cells register here by key, and the table either sorts Items
// itself or - for a stream or a provider it cannot reorder - reports the sort for the source to
// apply.
public partial class Table<TItem>
{
    private readonly Dictionary<string, TableSortHeader<TItem>> _sortHeaders = [];
    private VetoTableSort? _sort;
    private VetoTableSort? _lastSortParameter;
    private IReadOnlyList<TItem>? _sorted;
    private IReadOnlyList<TItem>? _sortedFrom;
    private int _sortedCount;
    private VetoTableSort? _sortedBy;

    /// <summary>
    /// The column the rows are sorted by and which way, or null for the source's own order. Bind
    /// it to follow or set the sort - and, with AsyncItems or ItemsProvider, to sort the source.
    /// </summary>
    [Parameter]
    public VetoTableSort? Sort { get; set; }

    /// <summary>Raised when a sortable header is clicked.</summary>
    [Parameter]
    public EventCallback<VetoTableSort?> SortChanged { get; set; }

    /// <summary>The sort in force, for the header cells to show.</summary>
    internal VetoTableSort? CurrentSort => _sort;

    internal void Register ( string key, TableSortHeader<TItem> header ) => _sortHeaders[key] = header;

    internal void Unregister ( string key, TableSortHeader<TItem> header )
    {
        if (_sortHeaders.TryGetValue(key, out var registered) && ReferenceEquals(registered, header))
        {
            _sortHeaders.Remove(key);
        }
    }

    /// <summary>Whether the rows are held here, so the table can sort them itself.</summary>
    private bool SortsItself => AsyncItems is null && ItemsProvider is null;

    /// <summary>
    /// The rows in the order shown: <c>ItemList</c>, sorted when a column says so and the table
    /// holds the rows. Worked out once per change of list or sort, not once per render.
    /// </summary>
    private IReadOnlyList<TItem> Rows
    {
        get
        {
            if (_sort is null || !SortsItself
                || !_sortHeaders.TryGetValue(_sort.Key, out var header) || header.By is not { } by)
            {
                return ItemList;
            }

            if (_sorted is null || !ReferenceEquals(_sortedFrom, ItemList)
                || _sortedCount != ItemList.Count || _sortedBy != _sort)
            {
                // OrderBy is stable, so rows that tie keep the source's order.
                _sorted = _sort.Direction == VetoSortDirection.Ascending
                    ? [.. ItemList.OrderBy(by, Comparer<object?>.Default)]
                    : [.. ItemList.OrderByDescending(by, Comparer<object?>.Default)];
                _sortedFrom = ItemList;
                _sortedCount = ItemList.Count;
                _sortedBy = _sort;
            }

            return _sorted;
        }
    }

    /// <summary>Adopts <see cref="Sort"/> when the parent changes it - not on every re-render.</summary>
    private void AdoptSortParameter ()
    {
        if (Sort != _lastSortParameter)
        {
            _lastSortParameter = Sort;
            _sort = Sort;
        }
    }

    /// <summary>Ascending, then descending, then the source's own order.</summary>
    internal async Task SortByAsync ( string key )
    {
        _sort = _sort?.Key != key
            ? new VetoTableSort(key, VetoSortDirection.Ascending)
            : _sort.Direction == VetoSortDirection.Ascending
                ? new VetoTableSort(key, VetoSortDirection.Descending)
                : null;

        // A new order starts at the beginning.
        _shown = _pageSize;
        _scrollToTop = true;

        if (_page != 0)
        {
            _page = 0;
            await PageIndexChanged.InvokeAsync(0);
        }

        // The parent's bound field is updated by now, so a provider that reads it sorts by the
        // new column when it is asked again.
        await SortChanged.InvokeAsync(_sort);

        if (ItemsProvider is not null)
        {
            await RefreshAsync();
        }

        StateHasChanged();
    }
}
