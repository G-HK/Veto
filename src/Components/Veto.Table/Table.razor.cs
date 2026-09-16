using Microsoft.AspNetCore.Components;

namespace Veto.Components.Table;

/// <summary>
/// A templated table over a list of items.
/// </summary>
/// <remarks>
/// Still the original sketch: it renders header, row and footer templates and nothing more.
/// Sorting, paging, selection and an empty state are all outstanding - see
/// <c>docs/Roadmap.md</c>. It does not yet follow the component contract in
/// <c>docs/Contributing.md</c>, so it takes no <c>Class</c>, <c>TestId</c> or splatted attributes.
/// </remarks>
/// <typeparam name="TItem">The type of the items being listed.</typeparam>
public partial class Table<TItem>
{
    /// <summary>Content for the table's header row.</summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Content for each row, given the item it represents.</summary>
    [Parameter]
    public RenderFragment<TItem> RowTemplate { get; set; } = default!;

    /// <summary>Content for the table's footer row.</summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>The items to list, one row each.</summary>
    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = default!;
}
