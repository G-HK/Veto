using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// A surface that groups related content, with optional header and footer regions. The
/// general-purpose content box - dashboard tiles, list rows, panels.
/// </summary>
/// <remarks>
/// Setting <see cref="OnClick"/> makes the whole card activate, and it then renders as a
/// <c>&lt;button&gt;</c> rather than a <c>&lt;div&gt;</c> so it is focusable, keyboard
/// operable and announced as a control - a clickable div would be none of those.
/// </remarks>
public partial class VetoCard : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-card";

    /// <summary>The card's main content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content for the header region. Takes precedence over <see cref="Title"/>.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>Content for the footer region - actions, metadata.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>A plain-text heading, rendered when no <see cref="Header"/> is given.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>How much the card lifts off the page.</summary>
    [Parameter]
    public VetoElevation Elevation { get; set; } = VetoElevation.Outlined;

    /// <summary>Padding inside each region.</summary>
    [Parameter]
    public VetoSpacing Padding { get; set; } = VetoSpacing.Large;

    /// <summary>Raised when an interactive card is activated by click, Enter or Space.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Disables an interactive card. Has no effect on a plain one.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private bool IsInteractive => OnClick.HasDelegate;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Elevation.ToString().ToLowerInvariant()))
            .AddClass(Modifier($"padding-{Padding.ToModifier()}"))
            .AddClass(Modifier("interactive"), IsInteractive)
            .AddClass(Modifier("disabled"), IsInteractive && Disabled);
}

/// <summary>How far a surface sits above the page.</summary>
public enum VetoElevation
{
    /// <summary>No border and no shadow; sits flat on the background.</summary>
    Flat,

    /// <summary>A hairline border, no shadow. The default.</summary>
    Outlined,

    /// <summary>A soft shadow, no border.</summary>
    Raised
}
