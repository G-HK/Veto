using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Toast;

/// <summary>
/// Everything one toast can be told about itself. Every property is nullable or has a default,
/// so a caller sets only what differs from the host's settings - anything left alone falls back
/// to <see cref="VetoToastHost"/>.
/// </summary>
/// <remarks>
/// The service takes one of these on every call, so a single toast can override the position,
/// the animation, the duration or the dismissal rules without changing anything globally.
/// </remarks>
/// <example>
/// <code>
/// Toasts.Error("Upload failed", new VetoToastOptions
/// {
///     Sticky = true,                       // stays until the user dismisses it
///     Position = VetoToastPosition.BottomCenter,
///     Animation = VetoToastAnimation.FlipXUp,
/// });
/// </code>
/// </example>
public sealed class VetoToastOptions
{
    /// <summary>A heading above the message. No title element is rendered without it.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Where this toast goes. Null uses the host's position, which is the normal case - mixing
    /// positions in one app is usually a mistake.
    /// </summary>
    public VetoToastPosition? Position { get; set; }

    /// <summary>How this toast arrives and leaves. Null uses the host's animation.</summary>
    public VetoToastAnimation? Animation { get; set; }

    /// <summary>
    /// How long the toast stays, in milliseconds. Null uses the host's duration. Ignored when
    /// <see cref="Sticky"/> is set.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// When true the toast never hides itself - it stays on screen until the user dismisses it
    /// or the code clears it. For anything the user must actually see.
    /// </summary>
    public bool Sticky { get; set; }

    /// <summary>
    /// Whether a bar counts the toast's remaining time down. Null uses the host's setting.
    /// Never drawn on a sticky toast, which has nothing to count.
    /// </summary>
    public bool? ShowProgress { get; set; }

    /// <summary>Whether the toast carries a close button. Null uses the host's setting.</summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>Whether clicking anywhere on the toast dismisses it. Null uses the host's setting.</summary>
    public bool? CloseOnClick { get; set; }

    /// <summary>
    /// Whether the countdown pauses while the pointer is over the toast. Null uses the host's
    /// setting. On by default there, so a toast is never snatched away mid-read.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>SVG markup for the toast's icon. Null uses the icon for its <see cref="VetoToastType"/>.</summary>
    public string? Icon { get; set; }

    /// <summary>When true no icon is drawn, whatever <see cref="Icon"/> says.</summary>
    public bool HideIcon { get; set; }

    /// <summary>
    /// Content rendered in place of the plain message - the escape hatch for a toast that needs
    /// markup of its own, such as an action link or a formatted figure.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>Extra CSS classes on the toast, for a bespoke look.</summary>
    public string? Class { get; set; }

    /// <summary>Inline style on the toast.</summary>
    public string? Style { get; set; }

    /// <summary>
    /// Rendered as <c>data-testid</c> on the toast. Toasts appear and vanish, so a spec needs a
    /// stable hook to catch one.
    /// </summary>
    public string? TestId { get; set; }

    /// <summary>
    /// A key that decides whether two toasts count as the same for duplicate handling. Null
    /// falls back to the type, title and message together.
    /// </summary>
    public string? DeduplicationKey { get; set; }

    /// <summary>
    /// What to do when this toast's message is already on screen. Null uses the host's setting.
    /// </summary>
    public VetoToastDuplicates? Duplicates { get; set; }

    /// <summary>Raised when the toast is clicked.</summary>
    public Func<Task>? OnClick { get; set; }

    /// <summary>Raised once the toast has gone, whether it timed out or was dismissed.</summary>
    public Func<Task>? OnClose { get; set; }

    /// <summary>The size of the toast. Null uses the host's size.</summary>
    public VetoSize? Size { get; set; }
}
