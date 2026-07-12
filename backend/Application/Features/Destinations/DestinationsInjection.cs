using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Destinations;

public static class DestinationsInjection
{
    /// <summary>
    /// Registers application-layer services specific to the Destinations feature.
    /// Provider implementations (OpenTripMap, Foursquare) are registered in the
    /// Infrastructure layer via <c>AddProviders</c>.
    /// </summary>
    public static void AddDestinationsFeature(this IServiceCollection collection)
    {
        // MediatR picks up the query handlers automatically via assembly scanning.
        // No additional registrations are needed at the application layer.
    }
}
