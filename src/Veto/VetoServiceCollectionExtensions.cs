using Microsoft.Extensions.DependencyInjection;
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
    /// <para>
    /// This is the meta package's convenience over the individual ones: it calls every
    /// component package's own registration for you. Installing components one by one
    /// instead? Call theirs directly - <see cref="VetoThemingServiceCollectionExtensions.AddVetoTheming"/>
    /// and <see cref="VetoToastServiceCollectionExtensions.AddVetoToast"/> are the only two
    /// packages that register anything.
    /// </para>
    /// </summary>
    /// <param name="services">The app's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVeto ( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddVetoTheming();
        services.AddVetoToast();

        return services;
    }
}