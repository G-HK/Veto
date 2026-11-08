using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Components.Menu;

/// <summary>
/// A menu opened at the pointer, for a right-click. It has no trigger of its own: the element
/// it belongs to calls <see cref="Open"/> from its <c>contextmenu</c> event.
/// </summary>
/// <remarks>
/// Filled with <see cref="VetoMenuItem"/>, <see cref="VetoMenuLabel"/> and
/// <see cref="VetoMenuDivider"/> as a <see cref="VetoMenu"/> is. Choosing an item runs its
/// <c>OnClick</c> and closes the menu; Escape or a click elsewhere closes it too, and the arrow
/// keys move between items. <c>VetoButton</c> wires one up through its <c>ContextMenu</c>.
/// </remarks>
/// <example>
/// <code>
/// &lt;div @oncontextmenu="args =&gt; _menu.Open(args)" @oncontextmenu:preventDefault&gt;…&lt;/div&gt;
///
/// &lt;VetoContextMenu @ref="_menu"&gt;
///     &lt;VetoMenuItem Text="Rename" OnClick="Rename" /&gt;
/// &lt;/VetoContextMenu&gt;
/// </code>
/// </example>
public partial class VetoContextMenu : VetoComponentBase, IAsyncDisposable
{
    private (double X, double Y)? _at;
    private bool _opening;
    private bool _closing;
    private IJSObjectReference? _module;

    /// <inheritdoc />
    protected override string RootClass => "veto-context-menu";

    /// <summary>The menu's items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The menu's accessible name.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether the menu is showing.</summary>
    public bool IsOpen => _at is not null;

    /// <summary>Opens the menu at the pointer position of <paramref name="args"/>.</summary>
    public void Open ( MouseEventArgs args )
    {
        _at = (args.ClientX, args.ClientY);
        _opening = true;
        StateHasChanged();
    }

    /// <summary>Closes the menu and hands focus back to where it was before it opened.</summary>
    public void Close ()
    {
        if (_at is null)
        {
            return;
        }

        _at = null;
        _closing = true;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        if (_opening)
        {
            _opening = false;
            await InvokeModuleAsync("openMenu", RootElement);
        }
        else if (_closing)
        {
            _closing = false;
            await InvokeModuleAsync("restoreFocus");
        }
    }

    private async Task OnKeyDownAsync ( KeyboardEventArgs args )
    {
        switch (args.Key)
        {
            case "Escape" or "Tab":
                Close();
                break;
            case "ArrowDown":
                await InvokeModuleAsync("moveMenuFocus", RootElement, 1);
                break;
            case "ArrowUp":
                await InvokeModuleAsync("moveMenuFocus", RootElement, -1);
                break;
            case "Home":
                await InvokeModuleAsync("focusEdge", RootElement, true);
                break;
            case "End":
                await InvokeModuleAsync("focusEdge", RootElement, false);
                break;
            case { Length: 1 } key when char.IsLetterOrDigit(key[0]):
                await InvokeModuleAsync("focusByLetter", RootElement, key);
                break;
        }
    }

    private async Task InvokeModuleAsync ( string function, params object?[] args )
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Menu/veto-context-menu.js");
            await _module.InvokeVoidAsync(function, args);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or a page that has gone away. The menu still works by pointer.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}