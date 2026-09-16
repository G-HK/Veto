using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Veto.Components.Toast;
using Veto.Theming;

namespace Veto;

/// <summary>Registers Veto's services with the app's dependency injection container.</summary>
public static class VetoServiceCollectionExtensions
{
    /// <summary>
    /// Adds every service Veto's components need. Call it in <c>Program.cs</c>:
    /// <code>builder.Services.AddVeto();</code>
    /// In a Blazor Web App this belongs in <b>both</b> the server and the client project, so
    /// the components resolve during prerendering as well as after WebAssembly takes over.
    /// </summary>
    /// <param name="services">The app's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVeto ( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, not singleton: in a server-rendered circuit the theme belongs to one user's
        // session, and a singleton would leak one visitor's choice to everyone.
        services.TryAddScoped<IVetoThemeService, VetoThemeService>();

        // Same reasoning for toasts: the queue belongs to one user's session. A singleton
        // would show one visitor's notifications to everyone else on the server.
        services.TryAddScoped<VetoToastService>();
        services.TryAddScoped<IVetoToastService>(sp => sp.GetRequiredService<VetoToastService>());

        return services;
    }
}
