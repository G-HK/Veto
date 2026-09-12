using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Veto.Core;
using Veto.Layouts.Shell;

namespace Veto.Layouts.Dock;

/// <summary>
/// A floating navigation bar that sits over the content, in the manner of a desktop dock or
/// taskbar. Attaches to any edge, in either of two appearances.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VetoDockAppearance.Magnified"/> is the macOS dock: items swell as the pointer
/// approaches and the swell falls away across their neighbours.
/// <see cref="VetoDockAppearance.Flat"/> is the Windows taskbar: fixed-size items with running
/// indicators. Everything else - position, auto-hide, keyboard handling - is shared.
/// </para>
/// <para>
/// Magnification needs the pointer's continuous position, not merely which item is hovered, so
/// it is computed in JavaScript and applied through a custom property. It is dropped entirely
/// under reduced motion and for coarse pointers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoDock Position="VetoAnchor.Bottom"&gt;
///     &lt;VetoDockItem Icon="@VetoIcons.Home" Label="Home" Href="/" /&gt;
///     &lt;VetoDockItem Icon="@VetoIcons.Search" Label="Search" OnClick="Find" /&gt;
/// &lt;/VetoDock&gt;
/// </code>
/// </example>
public partial class VetoDock : VetoComponentBase, IAsyncDisposable
{
    private readonly List<VetoDockItem> _items = [];

    private IJSObjectReference? _module;
    private bool _attached;
    private int _focusedIndex;

    /// <inheritdoc />
    protected override string RootClass => "veto-dock";

    /// <summary>The dock's items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Which edge the dock is attached to.</summary>
    [Parameter]
    public VetoAnchor Position { get; set; } = VetoAnchor.Bottom;

    /// <summary>Whether items magnify under the pointer or stay a fixed size.</summary>
    [Parameter]
    public VetoDockAppearance Appearance { get; set; } = VetoDockAppearance.Magnified;

    /// <summary>
    /// How much the item under the pointer grows, as a multiplier. Only applies to
    /// <see cref="VetoDockAppearance.Magnified"/>.
    /// </summary>
    [Parameter]
    public double Magnification { get; set; } = 1.75;

    /// <summary>
    /// How far the magnification reaches, in pixels. Wider values spread the swell across more
    /// neighbours; narrower ones make it hug the pointer.
    /// </summary>
    [Parameter]
    public double Range { get; set; } = 110;

    /// <summary>
    /// Slides the dock off the edge until the pointer approaches it. The reveal strip stays
    /// behind, so it can always be brought back.
    /// </summary>
    [Parameter]
    public bool AutoHide { get; set; }

    /// <summary>Distance from the edge of the screen, as a CSS length.</summary>
    [Parameter]
    public string Offset { get; set; } = "1rem";

    /// <summary>The resting size of an item, as a CSS length.</summary>
    [Parameter]
    public string ItemSize { get; set; } = "3rem";

    /// <summary>The dock's accessible name.</summary>
    [Parameter]
    public string Label { get; set; } = "Dock";

    /// <summary>True when the dock runs down a side rather than across the top or bottom.</summary>
    internal bool IsVertical => Position.IsVertical();

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Position.ToModifier()))
            .AddClass(Modifier(IsVertical ? "vertical" : "horizontal"))
            .AddClass(Modifier(Appearance.ToString().ToLowerInvariant()))
            .AddClass(Modifier("auto-hide"), AutoHide);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-dock-offset", Offset)
            .AddStyle("--veto-dock-item-size", ItemSize);

    /// <summary>Called by <see cref="VetoDockItem"/> as it initialises, to join the keyboard order.</summary>
    internal void Register ( VetoDockItem item )
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    /// <summary>Called by <see cref="VetoDockItem"/> as it is disposed.</summary>
    internal void Unregister ( VetoDockItem item )
    {
        var index = _items.IndexOf(item);

        if (index < 0)
        {
            return;
        }

        _items.Remove(item);

        // Keep the roving tab stop on a real item after one is removed.
        _focusedIndex = Math.Clamp(_focusedIndex, 0, Math.Max(0, _items.Count - 1));
    }

    /// <summary>
    /// Whether an item is the dock's single tab stop. A toolbar takes one tab stop and moves
    /// between its items with the arrow keys, rather than making the user tab through every one.
    /// </summary>
    internal bool IsTabStop ( VetoDockItem item )
    {
        var index = _items.IndexOf(item);
        return index < 0 ? false : index == Math.Clamp(_focusedIndex, 0, _items.Count - 1);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        var shouldMagnify = Appearance == VetoDockAppearance.Magnified && Visible;

        if (shouldMagnify && !_attached)
        {
            await AttachAsync();
        }
        else if (!shouldMagnify && _attached)
        {
            await DetachAsync();
        }
    }

    private async Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        if (_items.Count == 0)
        {
            return;
        }

        var previous = IsVertical ? "ArrowUp" : "ArrowLeft";
        var next = IsVertical ? "ArrowDown" : "ArrowRight";

        var target = args.Key switch
        {
            _ when args.Key == previous => _focusedIndex - 1,
            _ when args.Key == next => _focusedIndex + 1,
            "Home" => 0,
            "End" => _items.Count - 1,
            _ => -1
        };

        if (target < 0 && args.Key is not ("Home" or "End"))
        {
            return;
        }

        // Wraps, so holding an arrow key cycles rather than stopping dead at the end.
        _focusedIndex = (target + _items.Count) % _items.Count;

        StateHasChanged();
        await _items[_focusedIndex].FocusAsync();
    }

    private async Task AttachAsync ()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-dock.js");
            await _module.InvokeVoidAsync("attach", RootElement, new
            {
                magnification = Magnification,
                range = Range,
                vertical = IsVertical
            });

            _attached = true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or a page that has gone away. The dock still works; it just does not
            // magnify.
        }
    }

    private async Task DetachAsync ()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detach", RootElement);
            _attached = false;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Nothing to detach from a page that is already gone.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        if (_module is not null)
        {
            try
            {
                if (_attached)
                {
                    await _module.InvokeVoidAsync("detach", RootElement);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit or page is already gone.
            }
        }
    }
}

/// <summary>How a <see cref="VetoDock"/>'s items behave under the pointer.</summary>
public enum VetoDockAppearance
{
    /// <summary>Items swell as the pointer nears them, in the manner of the macOS dock.</summary>
    Magnified,

    /// <summary>Items stay a fixed size, in the manner of the Windows taskbar.</summary>
    Flat
}
