using Microsoft.AspNetCore.Components;

namespace Veto.Core;

/// <summary>
/// Base class for every Veto component that is not a form field - buttons, tables,
/// tooltips, layout pieces. Supplies the <see cref="IVetoComponent"/> parameter surface
/// and the class/style plumbing each component's markup binds to.
/// </summary>
/// <remarks>
/// Form components cannot use this base: they must inherit Blazor's
/// <c>InputBase&lt;T&gt;</c>, so they derive from <see cref="VetoInputBase{TValue}"/>
/// instead. Both delegate to <see cref="VetoComponentCore"/>, which is where the shared
/// behaviour actually lives.
/// </remarks>
public abstract class VetoComponentBase : ComponentBase, IVetoComponent
{
    private VetoComponentCore? _core;

    /// <inheritdoc />
    [Parameter]
    public string? Class { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? Style { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? Id { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? TestId { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <inheritdoc />
    [Parameter( CaptureUnmatchedValues = true )]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Glow { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? GlowColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Ping { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? PingColor { get; set; }

    /// <summary>
    /// The effects the consumer asked for, as one value. Components do not normally touch this -
    /// it is applied for them - but one that renders its root somewhere unusual can place it
    /// deliberately.
    /// </summary>
    internal VetoEffects Effects => new(Glow, GlowColor, Ping, PingColor);

    /// <summary>
    /// The component's own base class name, e.g. <c>veto-button</c>. Modifier classes are
    /// built from it, so keep it lowercase and hyphenated.
    /// </summary>
    protected abstract string RootClass { get; }

    /// <summary>The root element, captured with <c>@ref</c> so <see cref="FocusAsync"/> works.</summary>
    protected ElementReference RootElement { get; set; }

    /// <summary>The id rendered on the root element; generated when <see cref="Id"/> is null.</summary>
    protected string ElementId => Core.ResolveId(Id, AdditionalAttributes);

    /// <summary>The final <c>class</c> value for the root element.</summary>
    protected string? CssClass => BuildCssClass().Build();

    /// <summary>The final <c>style</c> value for the root element.</summary>
    protected string? CssStyle => BuildStyle().Build();

    /// <summary>
    /// The splatted attributes with <c>class</c> and <c>style</c> removed, for applying to an
    /// inner element when the root has already consumed them.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? InnerAttributes
        => VetoComponentCore.WithoutClassAndStyle(AdditionalAttributes);

    private VetoComponentCore Core => _core ??= new VetoComponentCore(RootClass);

    /// <summary>Builds <c>&lt;root-class&gt;--&lt;suffix&gt;</c>, e.g. <c>veto-button--lg</c>.</summary>
    protected string Modifier ( string suffix ) => Core.Modifier(suffix);

    /// <summary>
    /// Builds the root class list. Override to add modifier classes - add them to the builder
    /// returned by <see cref="StartCssClass"/>, then let the base append the consumer's classes last.
    /// </summary>
    protected virtual CssBuilder BuildCssClass ()
        => VetoComponentCore.BuildCssClass(StartCssClass(), Class, AdditionalAttributes, Effects);

    /// <summary>
    /// The component's own classes, before the consumer's are appended. Override this - rather
    /// than <see cref="BuildCssClass"/> - to add modifiers such as size and variant.
    /// </summary>
    protected virtual CssBuilder StartCssClass () => CssBuilder.Default(RootClass);

    /// <summary>Builds the root style list, appending the consumer's style last.</summary>
    protected virtual StyleBuilder BuildStyle ()
        => Core.BuildStyle(StartStyle(), Style, AdditionalAttributes, Effects);

    /// <summary>
    /// The component's own inline declarations, before the consumer's are appended. Override
    /// this - rather than <see cref="BuildStyle"/> - so a consumer's <c>Style</c> still wins.
    /// </summary>
    protected virtual StyleBuilder StartStyle () => StyleBuilder.Empty();

    /// <summary>Moves browser focus to the component's root element.</summary>
    public virtual ValueTask FocusAsync () => RootElement.FocusAsync();
}
