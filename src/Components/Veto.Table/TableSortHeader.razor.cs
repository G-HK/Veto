using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Table;

/// <summary>
/// A sortable header cell for <see cref="Table{TItem}"/>, written in its HeaderTemplate in place
/// of a plain <c>&lt;th&gt;</c>. Clicking cycles ascending, descending, then back to the
/// table's own order.
/// </summary>
/// <remarks>
/// With Items the table sorts by <see cref="By"/> itself. With AsyncItems or ItemsProvider the data
/// comes from somewhere the table cannot reorder, so it reports the sort through the table's
/// <c>Sort</c> and <c>SortChanged</c> and the source sorts - a new stream, or a provider that
/// reads <c>Sort</c>.
/// </remarks>
/// <typeparam name="TItem">The table's item type - inferred from the table.</typeparam>
public partial class TableSortHeader<TItem> : VetoComponentBase, IDisposable
{
    private string? _registeredKey;

    /// <summary>The table this header belongs to.</summary>
    [CascadingParameter]
    private Table<TItem>? Table { get; set; }

    /// <summary>The column's name, shown in the cell unless ChildContent replaces it, and its key by default.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;

    /// <summary>
    /// The value to sort by - <c>c =&gt; c.Price</c>. Needed when the table sorts Items itself;
    /// the values should be comparable (numbers, strings, dates).
    /// </summary>
    [Parameter]
    public Func<TItem, object?>? By { get; set; }

    /// <summary>The key reported in the table's <c>Sort</c>. Defaults to <see cref="Title"/>.</summary>
    [Parameter]
    public string? SortKey { get; set; }

    /// <summary>The cell's content, when it needs more than <see cref="Title"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The key this column is known by.</summary>
    internal string Key => SortKey ?? Title;

    /// <inheritdoc />
    protected override string RootClass => "veto-table__sort-header";

    private VetoSortDirection? Direction =>
        Table?.CurrentSort is { } sort && sort.Key == Key ? sort.Direction : null;

    private string? AriaSort => Direction switch
    {
        VetoSortDirection.Ascending => "ascending",
        VetoSortDirection.Descending => "descending",
        _ => null
    };

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (Table is null)
        {
            throw new InvalidOperationException($"{nameof(TableSortHeader<TItem>)} must be placed inside a Table's HeaderTemplate.");
        }

        // Registering only when the key changes: telling the table on every parameter set would
        // re-render it, which re-renders this header, which tells it again.
        if (_registeredKey != Key)
        {
            if (_registeredKey is not null)
            {
                Table.Unregister(_registeredKey, this);
            }

            Table.Register(Key, this);
            _registeredKey = Key;
        }
    }

    private Task ToggleAsync () => Table?.SortByAsync(Key) ?? Task.CompletedTask;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("ascending"), Direction == VetoSortDirection.Ascending)
            .AddClass(Modifier("descending"), Direction == VetoSortDirection.Descending);

    /// <inheritdoc />
    public void Dispose ()
    {
        if (_registeredKey is not null)
        {
            Table?.Unregister(_registeredKey, this);
        }

        GC.SuppressFinalize(this);
    }
}