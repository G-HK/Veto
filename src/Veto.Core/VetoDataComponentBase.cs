using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Veto.Core;

/// <summary>
/// Base class for Veto components that show a list of items - tables, lists, pickers. Adds the
/// item sources and row keying to everything <see cref="VetoComponentBase"/> already provides.
/// </summary>
/// <remarks>
/// <para>
/// Items come from one of three sources, and only one may be set. <see cref="Items"/> is a list
/// the app already holds. <see cref="AsyncItems"/> is a stream read a batch at a time, only as
/// far as the component asks (<see cref="ItemsWanted"/>) - which is what lets a table page
/// through, or scroll down, data that is still arriving. <see cref="ItemsProvider"/> answers
/// "rows 400 to 430, and how many in all?" - Blazor's <c>Virtualize</c> contract, for data too
/// large to hold, where only the rows in view are ever fetched.
/// </para>
/// <para>
/// Blazor matches parameters by name across the whole inheritance chain, so a derived component
/// must not declare its own <c>Items</c>, <c>AsyncItems</c>, <c>ItemsProvider</c> or
/// <c>ItemKey</c>: the first render
/// would throw.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The type of the items the component shows.</typeparam>
public abstract class VetoDataComponentBase<TItem> : VetoComponentBase, IAsyncDisposable
{
    private IAsyncEnumerable<TItem>? _source;
    private IAsyncEnumerator<TItem>? _enumerator;
    private CancellationTokenSource? _cancel;
    private List<TItem> _buffer = [];
    private Task _pending = Task.CompletedTask;
    private int _generation;

    /// <summary>The items to show, all at once. Null is treated as empty.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>
    /// The items to show, as a stream read only as far as the component needs - a page, or the
    /// next screenful. Pass a new stream to start over (a new filter, say); the old one is
    /// cancelled and disposed. Keep the reference stable between renders otherwise: a stream
    /// created inline, <c>AsyncItems="Load()"</c>, is a new stream on every render.
    /// </summary>
    [Parameter]
    public IAsyncEnumerable<TItem>? AsyncItems { get; set; }

    /// <summary>
    /// Fetches the items a range at a time - a start index and a count, answered with those items
    /// and the total. The derived component renders it through Blazor's <c>Virtualize</c>, which
    /// asks only for the rows in view and cancels requests the user has scrolled past. The base
    /// never calls it itself, so <see cref="ItemList"/> stays empty with a provider.
    /// </summary>
    [Parameter]
    public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// A stable key for each item, used as its <c>@key</c> so items keep their DOM (and any state
    /// inside them) when the list is re-ordered. Keys must be unique. Unset, items are not keyed
    /// at all - the item itself is not used, since a list may hold the same item twice.
    /// </summary>
    [Parameter]
    public Func<TItem, object?>? ItemKey { get; set; }

    /// <summary>
    /// The items available so far: all of <see cref="Items"/>, or as much of
    /// <see cref="AsyncItems"/> as has been read. Render from this rather than from
    /// <see cref="Items"/>, so a lazy query is not run twice.
    /// </summary>
    protected IReadOnlyList<TItem> ItemList { get; private set; } = [];

    /// <summary>Whether the stream has items not read yet. Always false for <see cref="Items"/>.</summary>
    protected bool HasMoreItems { get; private set; }

    /// <summary>Whether a batch is being read from the stream right now.</summary>
    protected bool IsLoadingItems { get; private set; }

    /// <summary>
    /// What the stream threw, if it failed. Reading stops there: a failed stream cannot be
    /// resumed, only replaced.
    /// </summary>
    protected Exception? ItemsError { get; private set; }

    /// <summary>
    /// How many items the component needs read right now - a page and one more, say, or what
    /// is scrolled into view. All of them, unless a derived component says otherwise.
    /// </summary>
    protected virtual int ItemsWanted => int.MaxValue;

    /// <summary>Called when a new <see cref="AsyncItems"/> stream replaces the old one, before it is read.</summary>
    protected virtual void OnItemsSourceReset () { }

    /// <summary>The <c>@key</c> for an item, or null when <see cref="ItemKey"/> is unset.</summary>
    protected object? KeyOf ( TItem item ) => ItemKey?.Invoke(item);

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if ((Items is null ? 0 : 1) + (AsyncItems is null ? 0 : 1) + (ItemsProvider is null ? 0 : 1) > 1)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} was given more than one of Items, AsyncItems and ItemsProvider. Set one.");
        }

        if (AsyncItems is null)
        {
            StopStream();
            ItemList = Items switch
            {
                null => [],
                IReadOnlyList<TItem> list => list,
                _ => [.. Items]
            };
            HasMoreItems = false;
            ItemsError = null;
        }
        else if (!ReferenceEquals(AsyncItems, _source))
        {
            StopStream();
            _source = AsyncItems;
            _cancel = new CancellationTokenSource();
            _enumerator = AsyncItems.GetAsyncEnumerator(_cancel.Token);
            _buffer = [];
            ItemList = _buffer;
            HasMoreItems = true;
            ItemsError = null;
            OnItemsSourceReset();
        }
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync () => LoadItemsAsync(ItemsWanted);

    /// <summary>
    /// Reads the stream until <paramref name="count"/> items are available or it ends, then
    /// re-renders. Calls queue behind each other, so a scroll and a button press arriving
    /// together cannot read the same stream at once. Does nothing for <see cref="Items"/>.
    /// </summary>
    protected Task LoadItemsAsync ( int count )
    {
        var previous = _pending;
        _pending = ReadAsync(previous, count);
        return _pending;
    }

    private async Task ReadAsync ( Task previous, int count )
    {
        try
        {
            await previous;
        }
        catch
        {
            // Reported by the read that owned it.
        }

        var enumerator = _enumerator;
        var generation = _generation;

        if (enumerator is null || !HasMoreItems || _buffer.Count >= count)
        {
            return;
        }

        IsLoadingItems = true;
        StateHasChanged();

        try
        {
            while (_buffer.Count < count)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    if (generation == _generation)
                    {
                        HasMoreItems = false;
                    }

                    break;
                }

                if (generation != _generation)
                {
                    return;
                }

                _buffer.Add(enumerator.Current);
            }
        }
        catch (OperationCanceledException) when (generation != _generation)
        {
            // Replaced or disposed mid-read: the new stream owns the state now.
            return;
        }
        catch (Exception ex) when (generation == _generation)
        {
            ItemsError = ex;
            HasMoreItems = false;
        }
        finally
        {
            if (generation == _generation)
            {
                IsLoadingItems = false;
            }
        }

        if (generation == _generation)
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Cancels the current stream and disposes it once any read in flight has finished - an
    /// async iterator cannot be disposed while it is being read.
    /// </summary>
    private void StopStream ()
    {
        if (_enumerator is null)
        {
            return;
        }

        _generation++;
        _cancel?.Cancel();
        _ = DisposeAfterAsync(_enumerator, _cancel, _pending);

        _enumerator = null;
        _cancel = null;
        _source = null;
        _pending = Task.CompletedTask;
        IsLoadingItems = false;
    }

    private static async Task DisposeAfterAsync ( IAsyncEnumerator<TItem> enumerator, CancellationTokenSource? cancel, Task pending )
    {
        try
        {
            await pending;
        }
        catch
        {
            // Already reported, or cancelled on purpose.
        }

        try
        {
            await enumerator.DisposeAsync();
        }
        catch
        {
            // The app's stream failed while closing; nothing is listening any more.
        }

        cancel?.Dispose();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync ()
    {
        StopStream();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
