using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Veto.Core;

namespace Veto.Components.TextBox;

/// <summary>
/// A single-line text input with a label, helper text, and validation message.
/// This is the reference implementation of the Veto component contract - new
/// components should follow its shape (see <c>docs/Contributing.md</c>).
/// </summary>
/// <typeparam name="TValue">
/// The bound type. <see cref="string"/>, numeric types, <see cref="DateTime"/>,
/// <see cref="DateOnly"/>, <see cref="Guid"/>, enums and their nullable forms are
/// converted with Blazor's <see cref="BindConverter"/>.
/// </typeparam>
public partial class VetoTextBox<TValue> : VetoInputBase<TValue>
{
    /// <inheritdoc />
    protected override string RootClass => "veto-textbox";

    /// <summary>
    /// The input's <c>type</c> attribute: <c>text</c>, <c>email</c>, <c>password</c>,
    /// <c>search</c>, <c>tel</c>, <c>url</c>, or <c>number</c>.
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "text";

    /// <summary>
    /// When true the bound value updates on every keystroke; when false (the default)
    /// it updates when the field loses focus, matching plain <c>@bind-Value</c>.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; }

    /// <summary>
    /// The format passed to <see cref="BindConverter"/> when converting the value to and
    /// from its string form, e.g. <c>yyyy-MM-dd</c> for a date.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Raised after the bound value changes, with the new value.</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChanged { get; set; }

    private Task OnInputAsync ( ChangeEventArgs args )
        => Immediate ? SetCurrentValueAsync(args.Value?.ToString()) : Task.CompletedTask;

    private Task OnChangeAsync ( ChangeEventArgs args )
        => Immediate ? Task.CompletedTask : SetCurrentValueAsync(args.Value?.ToString());

    private async Task SetCurrentValueAsync ( string? value )
    {
        // CurrentValueAsString runs InputBase's parse + validation pipeline and raises
        // ValueChanged, so @bind-Value and the EditContext both stay in sync.
        CurrentValueAsString = value;

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString ( TValue? value )
        => Format is not null && value is IFormattable formattable
            ? formattable.ToString(Format, CultureInfo.InvariantCulture)
            : base.FormatValueAsString(value);

    /// <inheritdoc />
    protected override bool TryParseValueFromString (
        string? value,
        [MaybeNullWhen( false )] out TValue result,
        [NotNullWhen( false )] out string? validationErrorMessage )
    {
        var parsed = Format is null
            ? BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var converted)
            : BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out converted);

        if (parsed)
        {
            result = converted!;
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }
}
