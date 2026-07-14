namespace Domain.Messages;

public static class TripControllerMsg
{
    public struct CreateTrip
    {
        public const string NameRequired = "Trip.CreateTrip.NameRequired";
        public const string Exception = "Trip.CreateTrip.Exception";
    }

    public struct SetDates
    {
        public const string StartDateRequired = "Trip.SetDates.StartDateRequired";
        public const string EndDateRequired = "Trip.SetDates.EndDateRequired";
        public const string InvalidDateRange = "Trip.SetDates.InvalidDateRange";
        public const string Exception = "Trip.SetDates.Exception";
    }

    public struct AddDestination
    {
        public const string ItineraryDayIdRequired = "Trip.AddDestination.ItineraryDayIdRequired";
        public const string ProviderPlaceIdRequired = "Trip.AddDestination.ProviderPlaceIdRequired";
        public const string NameRequired = "Trip.AddDestination.NameRequired";
        public const string ItineraryDayNotFound = "Trip.AddDestination.ItineraryDayNotFound";
        public const string Exception = "Trip.AddDestination.Exception";
    }

    public struct RemoveDestination
    {
        public const string Exception = "Trip.RemoveDestination.Exception";
    }

    public struct GetTrips
    {
        public const string Exception = "Trip.GetTrips.Exception";
    }

    public struct GetDetail
    {
        public const string Exception = "Trip.GetDetail.Exception";
    }

    /// <summary>Shared code for missing-or-not-owned trips; prevents trip-ID enumeration.</summary>
    public const string NotFound = "Trip.NotFound";

    /// <summary>Warning code when setting dates unschedules destinations whose day was removed.</summary>
    public const string DatesDestinationsUnscheduled = "Trip.SetDates.DestinationsUnscheduled";
}
