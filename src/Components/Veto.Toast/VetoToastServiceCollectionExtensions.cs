using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Veto.Components.Toast;

/// <summary>Registers Veto's toast services with the app's dependency injection container.</summary>
public static class VetoToastServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services <c>VetoToastHost</c> needs. Call it in <c>Program.cs</c>:
    /// <code>builder.Services.AddVetoToast();</code>
    /// In a Blazor Web App this belongs in <b>both</b> the server and the client project, so
    /// the components resolve during prerendering as well as after WebAssembly takes over.
    /// Installing the <c>Veto</c> meta package instead? <c>AddVeto()</c> already calls this.
    /// </summary>
    /// <param name="services">The app's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVetoToast ( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, not singleton: the queue belongs to one user's session. A singleton
        // would show one visitor's notifications to everyone else on the server.
        services.TryAddScoped<VetoToastService>();
        services.TryAddScoped<IVetoToastService>(sp => sp.GetRequiredService<VetoToastService>());

        return services;
    }
}
