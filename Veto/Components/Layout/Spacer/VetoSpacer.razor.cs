using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Layout;

/// <summary>
/// Space between siblings. With no <see cref="Size"/> it takes whatever room is going, which
/// pushes the items either side of it apart - the usual way to send one item in a toolbar to
/// the far end. Give it a size and it becomes a fixed gap instead.
/// </summary>
/// <remarks>Purely presentational, so it is hidden from assistive technology.</remarks>
public partial class VetoSpacer : VetoComponentBase
{
    /// <inheritdoc />
    protected override string RootClass => "veto-spacer";

    /// <summary>
    /// A fixed size from the spacing scale. Left at <see cref="VetoSpacing.None"/> the spacer
    /// is flexible and absorbs the free space instead.
    /// </summary>
    [Parameter]
    public VetoSpacing Size { get; set; } = VetoSpacing.None;

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("fixed"), Size != VetoSpacing.None)
            .AddClass(Modifier($"size-{Size.ToModifier()}"), Size != VetoSpacing.None);
}
