using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Menu;

/// <summary>
/// One action in a <see cref="VetoMenu"/>. Renders as an anchor when <see cref="Href"/> is set,
/// so a menu of links stays real navigation - middle-click and "open in new tab" included.
/// </summary>
/// <remarks>
/// <para>
/// The item takes its appearance from the menu around it rather than from its own parameters,
/// so switching a menu to icon-only is one change in one place instead of one per item. An item
/// outside a menu still renders, with both its icon and its text.
/// </para>
/// <para>
/// <see cref="Text"/> is the item's accessible name whatever the appearance hides, which is
/// what makes icon-only a visual choice rather than an accessibility cost. Give an item with
/// <see cref="ChildContent"/> rather than <c>Text</c> a <see cref="Label"/> when the menu might
/// be shown icon-only - arbitrary markup cannot be read as a name.
/// </para>
/// </remarks>
public partial class VetoMenuItem : VetoComponentBase, IDisposable
{
    /// <inheritdoc />
    protected override string RootClass => "veto-menu-item";

    /// <summary>The enclosing menu. Null when the item is used on its own.</summary>
    [CascadingParameter]
    public VetoMenuContext? Menu { get; set; }

    /// <summary>The item's text. Also its accessible name when the appearance hides it.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>The item's content, for when <see cref="Text"/> is not enough.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The item's icon, normally a constant from <see cref="VetoIcons"/>.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Markup to render instead of <see cref="Icon"/> - an avatar, a colour swatch.</summary>
    [Parameter]
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// The accessible name, when neither <see cref="Text"/> nor the content supplies a usable
    /// one. Needed for an icon-only item built from <see cref="ChildContent"/>.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// A keyboard hint shown at the end of the item, e.g. <c>Ctrl+S</c>. Decoration only: the
    /// item does not bind the shortcut, the app does.
    /// </summary>
    [Parameter]
    public string? Shortcut { get; set; }

    /// <summary>
    /// The item's tooltip. Falls back to <see cref="Text"/> or <see cref="Label"/>, which is
    /// what tells a reader what an icon-only item does.
    /// </summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>
    /// Makes the item a checkable one, reported through <c>aria-checked</c> and drawn with a
    /// tick. Leave null for an ordinary action.
    /// </summary>
    [Parameter]
    public bool? Checked { get; set; }

    /// <summary>
    /// Marks the item as the current choice - the selected sort order, the active theme. Unlike
    /// <see cref="Checked"/> this is a highlight, not a checkbox.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>Draws the item in the danger colour. For delete, discard, revoke.</summary>
    [Parameter]
    public bool Destructive { get; set; }

    /// <summary>Disables the item. It stays visible and is skipped by the arrow keys.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Navigates here instead of raising <see cref="OnClick"/>, rendering an anchor.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>The link target, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>The link's <c>rel</c>, when <see cref="Href"/> is set.</summary>
    [Parameter]
    public string? Rel { get; set; }

    /// <summary>Raised when the item is chosen.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Keeps the menu open after this item is chosen, whatever the menu's own setting says -
    /// for an item that toggles something the reader will want to toggle again.
    /// </summary>
    [Parameter]
    public bool KeepOpen { get; set; }

    private VetoMenuAppearance Appearance => Menu?.Appearance ?? VetoMenuAppearance.IconAndText;

    /// <summary>
    /// Whether the icon column is drawn. Text-only menus drop it entirely rather than leaving a
    /// gap where an icon would be, so a menu of plain actions does not look indented.
    /// </summary>
    private bool ShowIcon => Appearance != VetoMenuAppearance.TextOnly
        && (Icon is not null || IconContent is not null);

    private bool ShowText => Appearance != VetoMenuAppearance.IconOnly;

    private string? AccessibleName => ShowText ? Label : Label ?? Text;

    private string? TooltipText => Tooltip ?? Text ?? Label;

    /// <inheritdoc />
    protected override void OnInitialized ()
    {
        base.OnInitialized();

        // Registration happens on initialise rather than on render, so the menu has the items
        // in document order before the first arrow key can arrive.
        Menu?.Register(this);
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("dense"), Menu?.Dense == true)
            .AddClass(Modifier("icon-only"), Appearance == VetoMenuAppearance.IconOnly)
            .AddClass(Modifier("no-icon"), !ShowIcon)
            .AddClass(Modifier("selected"), Selected)
            .AddClass(Modifier("checked"), Checked == true)
            .AddClass(Modifier("checkable"), Checked.HasValue)
            .AddClass(Modifier("destructive"), Destructive)
            .AddClass(Modifier("disabled"), Disabled);

    private async Task ChooseAsync ( MouseEventArgs args )
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync(args);

        if (!KeepOpen && Menu is not null)
        {
            await Menu.ItemChosenAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose () => Menu?.Unregister(this);
}