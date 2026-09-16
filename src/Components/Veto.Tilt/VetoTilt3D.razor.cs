using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Veto.Core;

namespace Veto.Components.Tilt;

/// <summary>
/// Tilts its content in three dimensions as the pointer moves across it, as though the surface
/// were being pushed. Wrap anything: a card, an image, a whole panel.
/// </summary>
/// <remarks>
/// <para>
/// The pointer is tracked in JavaScript, which writes two CSS custom properties that the
/// transform reads. Nothing crosses into .NET while the pointer moves - a per-frame interop
/// call would cost frames in WebAssembly and considerably more over a Server circuit.
/// </para>
/// <para>
/// The effect is switched off entirely for anyone who has asked for reduced motion, and for
/// coarse pointers, where there is no hover position for it to follow.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoTilt3D MaxTilt="14" Glare="true"&gt;
///     &lt;VetoCard Title="Hover me"&gt;Content&lt;/VetoCard&gt;
/// &lt;/VetoTilt3D&gt;
/// </code>
/// </example>
public partial class VetoTilt3D : VetoComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _attached;

    /// <inheritdoc />
    protected override string RootClass => "veto-tilt-3d";

    /// <summary>The content to tilt.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// How far the surface tilts at the edges, in degrees. Small values read as depth; past
    /// roughly 20 it starts to look like a fairground mirror.
    /// </summary>
    [Parameter]
    public double MaxTilt { get; set; } = 12;

    /// <summary>
    /// The depth of the perspective, as a CSS length. Shorter values exaggerate the effect
    /// because the vanishing point is nearer the surface.
    /// </summary>
    [Parameter]
    public string Perspective { get; set; } = "1000px";

    /// <summary>How much the surface grows while the pointer is over it. 1 leaves it unscaled.</summary>
    [Parameter]
    public double Scale { get; set; } = 1.02;

    /// <summary>Adds a soft highlight that follows the pointer, like light across a glossy surface.</summary>
    [Parameter]
    public bool Glare { get; set; }

    /// <summary>Whether the surface returns to flat when the pointer leaves. True by default.</summary>
    [Parameter]
    public bool Reset { get; set; } = true;

    /// <summary>How long the surface takes to settle when the pointer leaves, in milliseconds.</summary>
    [Parameter]
    public int ResetDuration { get; set; } = 400;

    /// <summary>Turns the effect off while leaving the content and its layout untouched.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier("glare"), Glare)
            .AddClass(Modifier("disabled"), Disabled);

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-tilt-perspective", Perspective)
            .AddStyle("--veto-tilt-reset-duration", $"{ResetDuration}ms");

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync ( bool firstRender )
    {
        if (Disabled || !Visible)
        {
            if (_attached)
            {
                await DetachAsync();
            }

            return;
        }

        if (!_attached)
        {
            await AttachAsync();
        }
    }

    private async Task AttachAsync ()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Veto/veto-tilt.js");
            await _module.InvokeVoidAsync("attach", RootElement, new
            {
                maxTilt = MaxTilt,
                scale = Scale,
                glare = Glare,
                reset = Reset
            });

            _attached = true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, or the page has gone. The content still renders; it simply sits flat.
        }
    }

    private async Task DetachAsync ()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detach", RootElement);
            _attached = false;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Nothing to detach from a page that is already gone.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        if (_module is not null)
        {
            try
            {
                if (_attached)
                {
                    await _module.InvokeVoidAsync("detach", RootElement);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit or page is already gone.
            }
        }
    }
}
