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

    /// <summary>The input's <c>maxlength</c>: the most characters the browser lets the user type.</summary>
    [Parameter]
    public int? MaxLength { get; set; }

    /// <summary>Shows a "length / MaxLength" counter under the field. Needs <see cref="MaxLength"/>.</summary>
    [Parameter]
    public bool ShowCounter { get; set; }

    /// <summary>Shows a button that clears the field while it has a value and can be edited.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>The clear button's accessible name.</summary>
    [Parameter]
    public string ClearLabel { get; set; } = "Clear";

    /// <summary>Text shown inside the field before the value, e.g. <c>$</c> or <c>https://</c>.</summary>
    [Parameter]
    public string? Prefix { get; set; }

    /// <summary>Text shown inside the field after the value, e.g. <c>kg</c> or <c>.com</c>.</summary>
    [Parameter]
    public string? Suffix { get; set; }

    // What is in the box right now. Differs from CurrentValueAsString while the user types
    // without Immediate, which is exactly when the counter and clear button still need it.
    private string? _typed;

    private string? DisplayText => _typed ?? CurrentValueAsString;

    private int TextLength => DisplayText?.Length ?? 0;

    private bool ShowClearButton => Clearable && !Disabled && !ReadOnly && !string.IsNullOrEmpty(DisplayText);

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();
        _typed = null;
    }

    private Task OnInputAsync ( ChangeEventArgs args )
    {
        _typed = args.Value?.ToString();
        return Immediate ? SetCurrentValueAsync(_typed) : Task.CompletedTask;
    }

    private Task OnChangeAsync ( ChangeEventArgs args )
    {
        _typed = null;
        return Immediate ? Task.CompletedTask : SetCurrentValueAsync(args.Value?.ToString());
    }

    private async Task ClearAsync ()
    {
        _typed = null;
        CurrentValue = default;

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }

        await FocusAsync();
    }

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
            : TryParseFormatted(value, Format, out converted);

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

    // BindConverter.TryConvertTo<T> has no format argument, so a date shown as "dd/MM/yyyy"
    // could be displayed but never read back. The date and time types have format-aware
    // overloads; everything else parses as before.
    private static bool TryParseFormatted ( string? value, string format, out TValue? result )
    {
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        var culture = CultureInfo.InvariantCulture;
        object? parsed;
        bool ok;

        if (type == typeof(DateTime))
        {
            ok = BindConverter.TryConvertToNullableDateTime(value, culture, format, out var v);
            parsed = v;
        }
        else if (type == typeof(DateTimeOffset))
        {
            ok = BindConverter.TryConvertToNullableDateTimeOffset(value, culture, format, out var v);
            parsed = v;
        }
        else if (type == typeof(DateOnly))
        {
            ok = BindConverter.TryConvertToNullableDateOnly(value, culture, format, out var v);
            parsed = v;
        }
        else if (type == typeof(TimeOnly))
        {
            ok = BindConverter.TryConvertToNullableTimeOnly(value, culture, format, out var v);
            parsed = v;
        }
        else
        {
            return BindConverter.TryConvertTo(value, culture, out result);
        }

        // An empty box is null: fine for DateTime?, a parse failure for DateTime.
        if (!ok || (parsed is null && type == typeof(TValue)))
        {
            result = default;
            return false;
        }

        result = (TValue?)parsed;
        return true;
    }
}
