namespace Veto.Components.Switch;

/// <summary>
/// The order a three-state switch walks its states in as it is activated. Only meaningful
/// when <see cref="VetoSwitch{TValue}.ThreeState"/> is true.
/// </summary>
public enum VetoSwitchOrder
{
    /// <summary>
    /// Off, then on, then indeterminate. The default: the first activation of an unset
    /// switch reads as a plain "turn it on".
    /// </summary>
    OffOnIndeterminate,

    /// <summary>
    /// Off, then indeterminate, then on. For filters where "unset" sits naturally between
    /// exclude and include.
    /// </summary>
    OffIndeterminateOn
}

/// <summary>Which side of the control the switch's label sits on.</summary>
public enum VetoSwitchLabelPlacement
{
    /// <summary>After the control, reading order label-last. The default.</summary>
    End,

    /// <summary>Before the control, so a column of switches lines its labels up on the left.</summary>
    Start,

    /// <summary>Above the control, matching how the other Veto inputs stack their labels.</summary>
    Top
}
