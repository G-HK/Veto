namespace Veto.Core;

/// <summary>
/// The single implementation of the <see cref="IVetoComponent"/> contract.
/// <para>
/// Veto has two base classes rather than one, because a form component must inherit
/// Blazor's <c>InputBase&lt;T&gt;</c> to get <c>@bind-Value</c>, the ambient
/// <c>EditContext</c> and validation for free - and C# allows only one base class.
/// Rather than write the shared behaviour twice, both
/// <see cref="VetoComponentBase"/> and <see cref="VetoInputBase{TValue}"/> hold one of
/// these and delegate to it, so id generation, class building, style building and
/// attribute filtering exist in exactly one place.
/// </para>
/// </summary>
/// <param name="rootClass">The owning component's base class name, e.g. <c>veto-textbox</c>.</param>
internal sealed class VetoComponentCore ( string rootClass )
{
    private static int _idCounter;

    private string? _generatedId;

    /// <summary>The owning component's base class name.</summary>
    public string RootClass { get; } = rootClass;

    /// <summary>
    /// Returns the id the consumer supplied - either through the <c>Id</c> parameter or as a
    /// plain <c>id</c> attribute - otherwise a generated one that stays stable for the lifetime
    /// of the owning component instance, so <c>for</c> and <c>aria-*</c> references never dangle.
    /// </summary>
    /// <remarks>
    /// The splatted attribute is honoured because a component renders its own <c>id</c> after the
    /// splat, which would otherwise silently discard <c>id="my-anchor"</c> and leave the anchor
    /// pointing at nothing.
    /// </remarks>
    public string ResolveId ( string? id, IReadOnlyDictionary<string, object>? attributes = null )
    {
        if (id is not null)
        {
            return id;
        }

        if (attributes is not null
            && attributes.TryGetValue("id", out var splatted)
            && splatted?.ToString() is { Length: > 0 } fromAttributes)
        {
            return fromAttributes;
        }

        return _generatedId ??= $"{RootClass}-{Interlocked.Increment(ref _idCounter)}";
    }

    /// <summary>Builds <c>&lt;root-class&gt;--&lt;suffix&gt;</c>, e.g. <c>veto-textbox--sm</c>.</summary>
    public string Modifier ( string suffix ) => $"{RootClass}--{suffix}";

    /// <summary>
    /// Starts the root class list: the root class, the shared effects, then the consumer's
    /// <c>Class</c>, then any <c>class</c> that arrived through splatted attributes.
    /// Component-specific modifiers are added by the caller before these, so consumer classes
    /// always win.
    /// </summary>
    public static CssBuilder BuildCssClass ( CssBuilder builder, string? @class, IReadOnlyDictionary<string, object>? attributes, VetoEffects effects = default )
        => effects
            .Apply(builder)
            .AddClass(@class)
            .AddClassFromAttributes(attributes);

    /// <summary>
    /// Finishes the root style list: the component's own declarations, then the consumer's
    /// <c>Style</c>, then any <c>style</c> from splatted attributes - so the consumer's values
    /// come last and win.
    /// </summary>
    public StyleBuilder BuildStyle ( StyleBuilder builder, string? style, IReadOnlyDictionary<string, object>? attributes, VetoEffects effects = default )
        => effects
            .Apply(builder)
            .AddRaw(style)
            .AddStyleFromAttributes(attributes);

    /// <summary>
    /// The splatted attributes with <c>class</c> and <c>style</c> removed, so a component can
    /// apply them to an inner element without duplicating what already went onto the root.
    /// Returns the original dictionary untouched when there is nothing to strip.
    /// </summary>
    public static IReadOnlyDictionary<string, object>? WithoutClassAndStyle ( IReadOnlyDictionary<string, object>? attributes )
    {
        if (attributes is null)
        {
            return null;
        }

        if (!attributes.ContainsKey("class") && !attributes.ContainsKey("style"))
        {
            return attributes;
        }

        return attributes
            .Where(a => a.Key is not ("class" or "style"))
            .ToDictionary(a => a.Key, a => a.Value);
    }
}
