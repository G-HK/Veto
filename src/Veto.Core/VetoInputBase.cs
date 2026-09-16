using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Veto.Core;

/// <summary>
/// Base class for every Veto form component. Inherits Blazor's
/// <see cref="InputBase{TValue}"/>, which is what supplies <c>@bind-Value</c>,
/// <c>ValueExpression</c>, the ambient <see cref="EditContext"/> and the
/// <see cref="FieldIdentifier"/> used for validation - none of that is re-implemented here.
/// <para>
/// Because C# allows only one base class, this cannot also derive from
/// <see cref="VetoComponentBase"/>. Both classes implement <see cref="IVetoComponent"/>
/// and delegate to the same <see cref="VetoComponentCore"/>, so the shared behaviour is
/// written once.
/// </para>
/// </summary>
/// <typeparam name="TValue">The type of the bound value.</typeparam>
public abstract class VetoInputBase<TValue> : InputBase<TValue>, IVetoComponent
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

    // AdditionalAttributes is inherited from InputBase<TValue>, which already declares it
    // with CaptureUnmatchedValues, and satisfies IVetoComponent as-is.

    /// <inheritdoc />
    [Parameter]
    public string? RootWidth { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootHeight { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootMinWidth { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootMaxWidth { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootMinHeight { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootMaxHeight { get; set; }

    /// <inheritdoc />
    [Parameter]
    public VetoSpacing? RootMargin { get; set; }

    /// <inheritdoc />
    [Parameter]
    public VetoSpacing? RootPadding { get; set; }

    /// <inheritdoc />
    [Parameter]
    public VetoOverflow? RootOverflow { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootTop { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootRight { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootBottom { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootLeft { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? RootTooltip { get; set; }

    /// <inheritdoc />
    [Parameter]
    public int? RootZIndex { get; set; }

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
    /// The box-level parameters as one value. Components do not normally touch this - it is
    /// applied for them - in the same way as <see cref="Effects"/>.
    /// </summary>
    internal VetoBox Box => new(
        RootWidth, RootHeight, RootMinWidth, RootMaxWidth, RootMinHeight, RootMaxHeight,
        RootMargin, RootPadding, RootOverflow,
        RootTop, RootRight, RootBottom, RootLeft,
        RootZIndex, PositionsItself);

    /// <summary>Text shown in the field's label. When null no label element is rendered.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Guidance shown under the field. Replaced by the validation message while the field is invalid.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>Placeholder text shown while the field is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>When true the field cannot be focused or edited.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>When true the field can be focused and copied from but not edited.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Marks the field as required: renders the required indicator and sets
    /// <c>aria-required</c>. Actual enforcement comes from the model's validation
    /// attributes, not from this flag.
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>The field's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The field's visual treatment.</summary>
    [Parameter]
    public VetoVariant Variant { get; set; } = VetoVariant.Outlined;

    /// <summary>
    /// The component's own base class name, e.g. <c>veto-textbox</c>. Modifier classes are
    /// built from it, so keep it lowercase and hyphenated.
    /// </summary>
    protected abstract string RootClass { get; }

    /// <summary>The input element, captured with <c>@ref</c> so <see cref="FocusAsync"/> works.</summary>
    protected ElementReference InputElement { get; set; }

    /// <summary>The id rendered on the input; generated when <see cref="Id"/> is null.</summary>
    protected string ElementId => Core.ResolveId(Id, AdditionalAttributes);

    /// <summary>Id of the helper-text element, derived from <see cref="ElementId"/>.</summary>
    protected string HelperId => $"{ElementId}-helper";

    /// <summary>Id of the validation-message element, derived from <see cref="ElementId"/>.</summary>
    protected string ErrorId => $"{ElementId}-error";

    /// <summary>The validation messages currently attached to this field.</summary>
    protected IEnumerable<string> ValidationMessages =>
        EditContext is not null
            ? EditContext.GetValidationMessages(FieldIdentifier)
            : [];

    /// <summary>True when the field currently has at least one validation message.</summary>
    protected bool HasErrors => ValidationMessages.Any();

    /// <summary>
    /// The value for <c>aria-describedby</c>: the error element while invalid, the helper
    /// element otherwise, or null when there is neither.
    /// </summary>
    protected string? DescribedBy => HasErrors
        ? ErrorId
        : !string.IsNullOrWhiteSpace(HelperText)
            ? HelperId
            : null;

    /// <summary>
    /// The final <c>class</c> value for the root element. Named <c>RootCssClass</c> rather than
    /// <c>CssClass</c> because <see cref="InputBase{TValue}"/> already defines the latter.
    /// </summary>
    protected string? RootCssClass => BuildCssClass().Build();

    /// <summary>The final <c>style</c> value for the root element.</summary>
    protected string? RootCssStyle => BuildStyle().Build();

    /// <summary>
    /// Blazor's per-field validation class (<c>modified</c>, <c>valid</c>, <c>invalid</c>) for
    /// the inner input element. Null when the component is used outside an <see cref="EditForm"/>.
    /// </summary>
    protected string? FieldValidationClass => EditContext?.FieldCssClass(FieldIdentifier);

    /// <summary>
    /// The splatted attributes with <c>class</c> and <c>style</c> removed, so they can be applied
    /// to the inner input element without duplicating what already went onto the root.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? InnerAttributes =>
        VetoComponentCore.WithRootAttributes(AdditionalAttributes, RootTooltip);

    private VetoComponentCore Core => _core ??= new VetoComponentCore(RootClass);

    /// <summary>Builds <c>&lt;root-class&gt;--&lt;suffix&gt;</c>, e.g. <c>veto-textbox--sm</c>.</summary>
    protected string Modifier ( string suffix ) => Core.Modifier(suffix);

    /// <summary>Builds the root class list, appending the consumer's classes last.</summary>
    protected virtual CssBuilder BuildCssClass () =>
        VetoComponentCore.BuildCssClass(StartCssClass(), Class, AdditionalAttributes, Effects);

    /// <summary>
    /// The component's own classes: root class plus the size, variant and state modifiers every
    /// Veto input shares. Override to add component-specific modifiers, calling
    /// <c>base.StartCssClass()</c> first.
    /// </summary>
    protected virtual CssBuilder StartCssClass () =>
        CssBuilder.Default(RootClass)
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Variant.ToModifier()))
            .AddClass(Modifier("disabled"), Disabled)
            .AddClass(Modifier("readonly"), ReadOnly)
            .AddClass(Modifier("required"), Required)
            .AddClass(Modifier("invalid"), HasErrors);

    /// <summary>
    /// Whether the component's own stylesheet gives its root a non-static <c>position</c>.
    /// Override to true in a component that positions itself - a fixed overlay, a sticky bar -
    /// so <see cref="ZIndex"/> does not overwrite that with <c>relative</c> and drop it back
    /// into the flow.
    /// </summary>
    protected virtual bool PositionsItself => false;

    /// <summary>Builds the root style list, appending the consumer's style last.</summary>
    protected virtual StyleBuilder BuildStyle () =>
        Core.BuildStyle(StartStyle(), Style, AdditionalAttributes, Effects, Box);

    /// <summary>
    /// The component's own inline declarations, before the consumer's are appended. Override
    /// this - rather than <see cref="BuildStyle"/> - so a consumer's <c>Style</c> still wins.
    /// </summary>
    protected virtual StyleBuilder StartStyle () => StyleBuilder.Empty();

    /// <summary>Moves browser focus to the input element.</summary>
    public virtual ValueTask FocusAsync () => InputElement.FocusAsync();
}