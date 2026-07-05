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

    /// <summary>
    /// Shared error code used whenever a trip does not exist OR does not belong to the caller.
    /// Returning the same code for both cases prevents trip-ID enumeration.
    /// </summary>
    public const string NotFound = "Trip.NotFound";

    /// <summary>
    /// Warning code returned when setting trip dates causes scheduled destinations to become
    /// unscheduled (moved to Saved Places) because their ItineraryDay was removed.
    /// </summary>
    public const string DatesDestinationsUnscheduled = "Trip.SetDates.DestinationsUnscheduled";
}
