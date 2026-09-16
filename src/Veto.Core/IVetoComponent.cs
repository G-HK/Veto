namespace Veto.Core;

/// <summary>
/// The baseline surface every Veto component exposes, regardless of which base
/// class it derives from. Form components derive from <see cref="VetoInputBase{TValue}"/>
/// (which must inherit Blazor's <c>InputBase&lt;T&gt;</c>) and everything else from
/// <see cref="VetoComponentBase"/>; this interface is what keeps the two in step.
/// </summary>
public interface IVetoComponent
{
    /// <summary>Extra CSS classes appended after the component's own classes.</summary>
    string? Class { get; set; }

    /// <summary>Inline styles appended after the component's own.</summary>
    string? Style { get; set; }

    /// <summary>
    /// The root element's <c>id</c>. When not supplied a stable one is generated so
    /// labels and <c>aria-*</c> references always have something to point at.
    /// </summary>
    string? Id { get; set; }

    /// <summary>Rendered as <c>data-testid</c>; the stable hook for end-to-end tests.</summary>
    string? TestId { get; set; }

    /// <summary>When false the component renders nothing at all.</summary>
    bool Visible { get; set; }

    /// <summary>
    /// Surrounds the component with a neon glow that breathes in and out. Off by default:
    /// a glow means "look here", and a page where everything glows says nothing.
    /// </summary>
    bool Glow { get; set; }

    /// <summary>
    /// The glow's colour - any CSS colour. Defaults to the component's own accent where it has
    /// one, and to the theme's accent everywhere else.
    /// </summary>
    string? GlowColor { get; set; }

    /// <summary>
    /// Sends a ring expanding out of the component's border and fading, over and over, to draw
    /// the eye to something that has changed. Off by default.
    /// </summary>
    bool Ping { get; set; }

    /// <summary>The ring's colour. Defaults to the same accent the glow uses.</summary>
    string? PingColor { get; set; }

    /// <summary>The root element's width, as a CSS length. Null leaves it to the stylesheet.</summary>
    /// <remarks>
    /// <para>
    /// This and the other <c>Root*</c> parameters are emitted as inline declarations on the
    /// root element, before the consumer's own <c>Style</c> so that still wins. They are the
    /// escape hatch for the one place a component has to be a particular size, sit somewhere,
    /// or stack above something - so that case needs no wrapper element whose only job is to
    /// hold a style attribute.
    /// </para>
    /// <para>
    /// They are prefixed because they have to be. Blazor matches parameters by name, case
    /// insensitively, across a component's whole inheritance chain: a derived
    /// <c>[Parameter]</c> called <c>Width</c> would not override an inherited one, it would
    /// collide with it, and the component would throw
    /// "declares more than one parameter matching the name" on its first render. Several
    /// components already own these names for something more specific - <c>VetoDrawer.Width</c>
    /// is the width the drawer opens to, <c>VetoCard.Padding</c> pads the regions inside the
    /// card - and those keep their names. The prefix also says which element is meant, which
    /// was the ambiguity anyway.
    /// </para>
    /// </remarks>
    string? RootWidth { get; set; }

    /// <summary>The root element's height, as a CSS length.</summary>
    string? RootHeight { get; set; }

    /// <summary>The root element's minimum width, as a CSS length.</summary>
    string? RootMinWidth { get; set; }

    /// <summary>The root element's maximum width, as a CSS length.</summary>
    string? RootMaxWidth { get; set; }

    /// <summary>The root element's minimum height, as a CSS length.</summary>
    string? RootMinHeight { get; set; }

    /// <summary>The root element's maximum height, as a CSS length.</summary>
    string? RootMaxHeight { get; set; }

    /// <summary>Space outside the root element, as a step on the shared spacing scale.</summary>
    VetoSpacing? RootMargin { get; set; }

    /// <summary>
    /// Space inside the root element, as a step on the shared spacing scale. Distinct from the
    /// <c>Padding</c> that <c>VetoCard</c> and <c>VetoContainer</c> own, which pads the regions
    /// inside those components rather than their root.
    /// </summary>
    VetoSpacing? RootPadding { get; set; }

    /// <summary>
    /// How the root element treats content that does not fit. Setting it to
    /// <see cref="VetoOverflow.Visible"/> is how a component that clips by default is made to
    /// let a popover out of it.
    /// </summary>
    VetoOverflow? RootOverflow { get; set; }

    /// <summary>
    /// The root element's offset from the top, as a CSS length. Setting any offset also makes
    /// the root positioned, since an offset on a static element does nothing.
    /// </summary>
    string? RootTop { get; set; }

    /// <summary>The root element's offset from the right, as a CSS length.</summary>
    string? RootRight { get; set; }

    /// <summary>The root element's offset from the bottom, as a CSS length.</summary>
    string? RootBottom { get; set; }

    /// <summary>The root element's offset from the left, as a CSS length.</summary>
    string? RootLeft { get; set; }

    /// <summary>
    /// The root element's native tooltip, rendered as its <c>title</c> attribute.
    /// </summary>
    /// <remarks>
    /// A native tooltip appears on hover only - no keyboard, no touch - so it is a convenience
    /// for a hint, never the only place something important is said. A component that renders
    /// its own <c>title</c> keeps it.
    /// </remarks>
    string? RootTooltip { get; set; }

    /// <summary>
    /// The root element's <c>z-index</c>, for the times a component has to be lifted above - or
    /// pushed below - something it overlaps. Null leaves the stacking order to the stylesheet.
    /// </summary>
    /// <remarks>
    /// <c>z-index</c> only means anything on a positioned element, so setting this also makes
    /// the root <c>position: relative</c> unless the component already positions itself. A
    /// relative element does not move, but it does become the containing block for any
    /// absolutely positioned descendant - which is normally what was wanted anyway.
    /// </remarks>
    int? RootZIndex { get; set; }

    /// <summary>
    /// Whether the component draws its border. Null - the default - leaves the component's own
    /// stylesheet in charge, which is what nearly every component wants.
    /// </summary>
    /// <remarks>
    /// False does not remove the border, it makes it transparent: the space it occupied stays,
    /// so turning a border off never moves the content by a pixel, and the focus and invalid
    /// states still have something to recolour. It is inherited, so a border on an element
    /// inside the component goes too.
    /// <para>
    /// Named <c>RootBordered</c> rather than <c>Bordered</c> because several components already
    /// have a <c>Bordered</c> of their own, and Blazor matches parameters by name across the
    /// whole inheritance chain - two of them is a first-render exception, not a compile error.
    /// </para>
    /// </remarks>
    bool? RootBordered { get; set; }

    /// <summary>
    /// Lifts the component off the page, as though it stood out of the screen. Off by default.
    /// </summary>
    /// <remarks>
    /// A perspective transform plus a shadow cast down and away, so the component reads as
    /// raised rather than merely shadowed. It is the component's own root that is lifted, so
    /// this composes with anything inside it.
    /// </remarks>
    bool Extrude { get; set; }

    /// <summary>
    /// How far the component stands out, as a CSS length. Null uses the effect's own default.
    /// </summary>
    string? ExtrudeDepth { get; set; }

    /// <summary>Unmatched attributes, splatted onto the root element.</summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}