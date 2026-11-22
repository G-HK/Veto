using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Veto.Components.Icon;
using Veto.Core;

namespace Veto.Components.Switch;

/// <summary>
/// A sliding on/off control, and - bound to a <see cref="bool"/>? with
/// <see cref="ThreeState"/> set - a three-state one that also carries an indeterminate
/// value for "not answered yet".
/// </summary>
/// <remarks>
/// <para>
/// <typeparamref name="TValue"/> must be <see cref="bool"/> or <see cref="bool"/>?;
/// anything else throws at first render rather than silently binding nothing. Three-state
/// needs <see cref="bool"/>?, because a <see cref="bool"/> has nowhere to put the third
/// state.
/// </para>
/// <para>
/// The two modes are deliberately different to assistive technology. A two-state switch is
/// an ARIA <c>switch</c>, which is only allowed to be on or off. A three-state one is an
/// ARIA <c>checkbox</c>, the only role whose <c>aria-checked</c> accepts <c>mixed</c>. The
/// visual treatment is the same either way; only the role and the announced state differ.
/// </para>
/// </remarks>
/// <typeparam name="TValue"><see cref="bool"/> for on/off, <see cref="bool"/>? for three-state.</typeparam>
/// <example>
/// <code>
/// &lt;VetoSwitch TValue="bool" Label="Notifications" @bind-Value="_enabled" /&gt;
/// &lt;VetoSwitch TValue="bool?" Label="Archived" ThreeState="true" @bind-Value="_archived" /&gt;
/// </code>
/// </example>
public partial class VetoSwitch<TValue> : VetoInputBase<TValue>
{
    private static readonly bool IsBool = typeof(TValue) == typeof(bool);
    private static readonly bool IsNullableBool = typeof(TValue) == typeof(bool?);

    /// <inheritdoc />
    protected override string RootClass => "veto-switch";

    /// <summary>
    /// When true the switch cycles through three states - off, on and indeterminate - instead
    /// of two. Requires <typeparamref name="TValue"/> to be <see cref="bool"/>?; the
    /// indeterminate state is <c>null</c>.
    /// </summary>
    [Parameter]
    public bool ThreeState { get; set; }

    /// <summary>The order a three-state switch walks its states in. Ignored when <see cref="ThreeState"/> is false.</summary>
    [Parameter]
    public VetoSwitchOrder Order { get; set; } = VetoSwitchOrder.OffOnIndeterminate;

    /// <summary>The accent the track takes when the switch is on.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>Which side of the control the label sits on.</summary>
    [Parameter]
    public VetoSwitchLabelPlacement LabelPlacement { get; set; } = VetoSwitchLabelPlacement.End;

    /// <summary>When true the thumb carries an icon for the current state.</summary>
    [Parameter]
    public bool ShowIcons { get; set; }

    /// <summary>When true the current state's text is rendered beside the control.</summary>
    [Parameter]
    public bool ShowStateText { get; set; }

    /// <summary>
    /// Text for the on state. Used by <see cref="ShowStateText"/>, and as the accessible name
    /// when no <c>Label</c> is supplied.
    /// </summary>
    [Parameter]
    public string OnText { get; set; } = "On";

    /// <summary>Text for the off state.</summary>
    [Parameter]
    public string OffText { get; set; } = "Off";

    /// <summary>Text for the indeterminate state.</summary>
    [Parameter]
    public string IndeterminateText { get; set; } = "Indeterminate";

    /// <summary>Thumb icon shown in the on state when <see cref="ShowIcons"/> is set.</summary>
    [Parameter]
    public string? OnIcon { get; set; } = VetoIcons.Check;

    /// <summary>Thumb icon shown in the off state when <see cref="ShowIcons"/> is set.</summary>
    [Parameter]
    public string? OffIcon { get; set; } = VetoIcons.Close;

    /// <summary>Thumb icon shown in the indeterminate state when <see cref="ShowIcons"/> is set.</summary>
    [Parameter]
    public string? IndeterminateIcon { get; set; } = VetoIcons.Minus;

    /// <summary>
    /// Asked before every change, with the state the switch is about to take. Return false to
    /// keep the current state - after a confirmation the user declined, or a save that failed.
    /// While it runs the switch is busy: it ignores further input and shows a spinner in the
    /// thumb.
    /// </summary>
    [Parameter]
    public Func<bool?, Task<bool>>? BeforeChange { get; set; }

    /// <summary>
    /// Shows the busy state without a <see cref="BeforeChange"/> - for a switch whose value is
    /// being loaded or saved elsewhere. Input is ignored while it is set.
    /// </summary>
    [Parameter]
    public bool Loading { get; set; }

    private bool _busy;

    private bool IsBusy => _busy || Loading;

    /// <summary>Raised after the bound value changes, with the new value.</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChanged { get; set; }

    /// <summary>The current state: true for on, false for off, null for indeterminate.</summary>
    public bool? State => (object?)CurrentValue as bool?;

    private string StateName => State switch
    {
        true => "on",
        false => "off",
        _ => "indeterminate"
    };

    private string StateText => State switch
    {
        true => OnText,
        false => OffText,
        _ => IndeterminateText
    };

    private string? ThumbIcon => State switch
    {
        true => OnIcon,
        false => OffIcon,
        _ => IndeterminateIcon
    };

    // A two-state switch is an ARIA switch; only checkbox may report aria-checked="mixed".
    private string Role => ThreeState ? "checkbox" : "switch";

    private string AriaChecked => State switch
    {
        true => "true",
        false => "false",
        _ => ThreeState ? "mixed" : "false"
    };

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    private string AccessibleName => StateText;

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        if (!IsBool && !IsNullableBool)
        {
            throw new InvalidOperationException(
                $"VetoSwitch binds to bool or bool? only, but TValue was {typeof(TValue)}.");
        }

        if (ThreeState && !IsNullableBool)
        {
            throw new InvalidOperationException(
                "ThreeState requires TValue to be bool?, because a bool has nowhere to hold the indeterminate state.");
        }
    }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier(StateName))
            .AddClass(Modifier("three-state"), ThreeState)
            .AddClass(Modifier("busy"), IsBusy)
            .AddClass(Modifier($"label-{LabelPlacement.ToString().ToLowerInvariant()}"));

    /// <summary>Moves to the next state, as if the user had activated the switch.</summary>
    public Task ToggleAsync () => SetStateAsync(NextState(State));

    /// <summary>Sets the switch to a given state, honouring <c>Disabled</c> and <c>ReadOnly</c>.</summary>
    /// <param name="state">True for on, false for off, null for indeterminate.</param>
    public async Task SetStateAsync ( bool? state )
    {
        if (Disabled || ReadOnly || IsBusy || state == State)
        {
            return;
        }

        if (BeforeChange is not null)
        {
            _busy = true;
            StateHasChanged();

            try
            {
                if (!await BeforeChange(state))
                {
                    return;
                }
            }
            finally
            {
                _busy = false;
            }
        }

        // CurrentValue runs InputBase's change pipeline, so @bind-Value, the EditContext and
        // field validation all stay in step - the same route VetoTextBox takes.
        CurrentValue = state is null ? default! : (TValue)(object)state.Value;

        if (OnValueChanged.HasDelegate)
        {
            await OnValueChanged.InvokeAsync(CurrentValue);
        }
    }

    private Task AdvanceAsync () => ToggleAsync();

    private Task OnKeyDownAsync ( KeyboardEventArgs args ) => args.Key switch
    {
        // Space and Enter already activate a button natively. The arrows are the extra
        // affordance a switch is expected to have: they set a state outright rather than
        // cycling, so any state of a three-state switch is one keypress away.
        "ArrowRight" or "End" => SetStateAsync(true),
        "ArrowLeft" or "Home" => SetStateAsync(false),
        "ArrowUp" or "ArrowDown" when ThreeState => SetStateAsync(null),
        _ => Task.CompletedTask
    };

    private bool? NextState ( bool? current )
    {
        if (!ThreeState)
        {
            return current is not true;
        }

        return Order == VetoSwitchOrder.OffOnIndeterminate
            ? current switch { false => true, true => null, _ => false }
            : current switch { false => null, null => true, _ => false };
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString (
        string? value,
        [MaybeNullWhen( false )] out TValue result,
        [NotNullWhen( false )] out string? validationErrorMessage )
    {
        if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out var converted))
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
