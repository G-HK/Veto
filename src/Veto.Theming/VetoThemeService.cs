using Microsoft.JSInterop;

namespace Veto.Theming;

/// <inheritdoc cref="IVetoThemeService" />
public sealed class VetoThemeService : IVetoThemeService, IAsyncDisposable
{
    private readonly IJSRuntime _js;

    private DotNetObjectReference<VetoThemeService>? _selfRef;
    private IJSObjectReference? _module;
    private bool _systemPrefersDark;
    private bool _initialized;

    /// <summary>Creates the service. Registered by <c>AddVeto()</c>; not constructed directly.</summary>
    public VetoThemeService ( IJSRuntime js ) => _js = js;

    /// <inheritdoc />
    public VetoThemeMode Mode { get; private set; } = VetoThemeMode.System;

    /// <inheritdoc />
    public VetoTheme Theme { get; private set; } = new();

    /// <inheritdoc />
    public bool IsDark => Mode switch
    {
        VetoThemeMode.Dark => true,
        VetoThemeMode.Light => false,
        _ => _systemPrefersDark
    };

    /// <inheritdoc />
    public event Func<Task>? Changed;

    /// <inheritdoc />
    public async Task InitializeAsync ()
    {
        if (_initialized)
        {
            return;
        }

        // Every JS call is guarded: during prerendering there is no browser to talk to, and
        // the theme simply stays at its default until the component renders interactively.
        // Only a successful import counts as initialised - marked up front, one attempt made
        // while prerendering would have blocked the real one for good.
        _module = await ImportModuleAsync();

        if (_module is null)
        {
            return;
        }

        _initialized = true;

        _selfRef = DotNetObjectReference.Create(this);
        var state = await _module.InvokeAsync<ThemeState>("initialize", _selfRef);

        Mode = Enum.TryParse<VetoThemeMode>(state.Mode, out var mode) ? mode : VetoThemeMode.System;
        _systemPrefersDark = state.PrefersDark;

        await NotifyAsync();
    }

    /// <inheritdoc />
    public async Task SetModeAsync ( VetoThemeMode mode )
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;

        _module ??= await ImportModuleAsync();

        if (_module is not null)
        {
            _systemPrefersDark = await _module.InvokeAsync<bool>("setMode", mode.ToString());
        }

        await NotifyAsync();
    }

    /// <inheritdoc />
    public Task ToggleAsync () => SetModeAsync(IsDark ? VetoThemeMode.Light : VetoThemeMode.Dark);

    /// <inheritdoc />
    public async Task SetThemeAsync ( VetoTheme theme )
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        await NotifyAsync();
    }

    /// <summary>
    /// Invoked from JavaScript when the operating system's colour-scheme preference changes.
    /// Not part of the public API despite being public - JS interop requires it.
    /// </summary>
    /// <param name="prefersDark">Whether the operating system now prefers dark.</param>
    [JSInvokable]
    public async Task OnSystemPreferenceChanged ( bool prefersDark )
    {
        if (_systemPrefersDark == prefersDark)
        {
            return;
        }

        _systemPrefersDark = prefersDark;

        // Only visible while following the system; in a forced mode nothing on screen changes.
        if (Mode == VetoThemeMode.System)
        {
            await NotifyAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync ()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit or page is already gone; nothing to clean up on the JS side.
            }
        }

        _selfRef?.Dispose();
    }

    private async Task<IJSObjectReference?> ImportModuleAsync ()
    {
        try
        {
            return await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Veto.Theming/veto-theme.js");
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // InvalidOperationException is what prerendering throws: JS interop is not
            // available until the component is interactive.
            return null;
        }
    }

    /// <summary>Called when another tab of the same site changes the stored mode.</summary>
    [JSInvokable]
    public async Task OnStoredModeChanged ( string mode )
    {
        var next = Enum.TryParse<VetoThemeMode>(mode, out var parsed) ? parsed : VetoThemeMode.System;

        if (next == Mode)
        {
            return;
        }

        Mode = next;
        await NotifyAsync();
    }

    // Each subscriber awaited in turn. Invoking a multicast Func<Task> directly returns only
    // the last handler's task, so every other handler ran unobserved and its exceptions were
    // lost.
    private async Task NotifyAsync ()
    {
        if (Changed is null)
        {
            return;
        }

        foreach (var handler in Changed.GetInvocationList().Cast<Func<Task>>())
        {
            await handler();
        }
    }

    private sealed record ThemeState ( string Mode, bool PrefersDark );
}