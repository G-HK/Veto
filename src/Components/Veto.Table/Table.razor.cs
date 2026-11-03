using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Core;

namespace Veto.Components.Table;

/// <summary>
/// A templated table over a list of items: you supply the header cells, a row template and an
/// optional footer, and the table draws the rows, an empty state, stripes, hover and a sticky
/// header.
/// </summary>
/// <remarks>
/// Sorting, paging, selection and virtualization are still outstanding - see
/// <c>docs/Roadmap.md</c>. They need a column model, which this templated shape does not have.
/// </remarks>
/// <typeparam name="TItem">The type of the items being listed.</typeparam>
public partial class Table<TItem> : VetoComponentBase
{
    private IReadOnlyList<TItem> _items = [];

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

    /// <summary>The items to list, one row each. Null is treated as empty.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

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

    /// <summary>
    /// A stable key for each item, used as the row's <c>@key</c> so rows keep their DOM (and any
    /// state inside them) when the list is re-ordered. Keys must be unique. Unset, rows are not
    /// keyed at all - the item itself is not used, since a list may hold the same item twice.
    /// </summary>
    [Parameter]
    public Func<TItem, object?>? ItemKey { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        // Materialised once per render: a lazy query would otherwise run twice, once for the
        // count and once for the rows.
        _items = Items switch
        {
            null => [],
            IReadOnlyList<TItem> list => list,
            _ => [.. Items]
        };
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("striped"), Striped)
            .AddClass(Modifier("bordered"), Bordered)
            .AddClass(Modifier("hover"), Hover)
            .AddClass(Modifier("dense"), Dense)
            .AddClass(Modifier("sticky"), StickyHeader)
            .AddClass(Modifier("clickable"), OnRowClick.HasDelegate);

    // A null key is no key, which is what lets @key be conditional.
    private object? KeyOf ( TItem item ) => ItemKey?.Invoke(item);

    private string RowCss ( TItem item )
    {
        var extra = RowClass?.Invoke(item);
        return string.IsNullOrWhiteSpace(extra) ? "veto-table__row" : $"veto-table__row {extra}";
    }

    private Task OnRowKeyDownAsync ( KeyboardEventArgs e, TItem item )
        => e.Key is "Enter" or " " ? OnRowClick.InvokeAsync(item) : Task.CompletedTask;
}
