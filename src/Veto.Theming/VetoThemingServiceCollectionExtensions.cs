using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Veto.Theming;

/// <summary>Registers Veto's theming services with the app's dependency injection container.</summary>
public static class VetoThemingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services <c>VetoThemeProvider</c> and <c>VetoThemeToggle</c> need. Call it in
    /// <c>Program.cs</c>:
    /// <code>builder.Services.AddVetoTheming();</code>
    /// In a Blazor Web App this belongs in <b>both</b> the server and the client project, so
    /// the components resolve during prerendering as well as after WebAssembly takes over.
    /// Installing the <c>Veto</c> meta package instead? <c>AddVeto()</c> already calls this.
    /// </summary>
    /// <param name="services">The app's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVetoTheming ( this IServiceCollection services )
    {
        services.TryAddScoped<IVetoThemeService, VetoThemeService>();
        return services;
    }
}