namespace Veto.Components.Gantt;

/// <summary>The first and last day on a <see cref="VetoGantt"/>'s axis.</summary>
/// <param name="Start">The first day on the axis.</param>
/// <param name="End">The last day on the axis.</param>
public readonly record struct VetoGanttRange ( DateOnly Start, DateOnly End );