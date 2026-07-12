using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Trips;

public static class TripsInjection
{
    /// <summary>
    /// Registers application-layer services for the Trips feature.
    /// MediatR picks up all command and query handlers automatically via assembly scanning;
    /// no additional registrations are required at this layer.
    /// </summary>
    public static void AddTripsFeature(this IServiceCollection collection)
    {
    }
}
