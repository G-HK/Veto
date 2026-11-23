namespace Veto.Components.Table;

/// <summary>Which way a sorted column runs.</summary>
public enum VetoSortDirection
{
    /// <summary>Smallest first: A to Z, oldest first, cheapest first.</summary>
    Ascending,

    /// <summary>Largest first.</summary>
    Descending
}

/// <summary>
/// How a <see cref="Table{TItem}"/> is sorted: the column, by its <c>SortKey</c> (its Title unless
/// set), and the direction. Null when the table is in its own order.
/// </summary>
/// <param name="Key">The sorted column's key.</param>
/// <param name="Direction">Ascending or descending.</param>
public sealed record VetoTableSort ( string Key, VetoSortDirection Direction );
