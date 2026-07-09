namespace Domain.Constants;

/// <summary>
/// Well-known external destination-provider names. Used as DI service keys and as the
/// value of the <c>Providers:Default</c> configuration entry.
/// </summary>
public static class ProviderNames
{
    public const string OpenTripMap = "OpenTripMap";
    public const string Foursquare = "Foursquare";

    public static readonly IReadOnlyList<string> All = [OpenTripMap, Foursquare];
}
