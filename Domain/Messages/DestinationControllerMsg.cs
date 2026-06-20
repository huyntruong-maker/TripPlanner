namespace Domain.Messages;

public static class DestinationControllerMsg
{
    public struct SearchLocations
    {
        public const string QueryRequired = "Destination.SearchLocations.QueryRequired";
        public const string Exception = "Destination.SearchLocations.Exception";
    }

    public struct GetAttractions
    {
        public const string LatitudeRequired = "Destination.GetAttractions.LatitudeRequired";
        public const string LongitudeRequired = "Destination.GetAttractions.LongitudeRequired";
        public const string InvalidCoordinates = "Destination.GetAttractions.InvalidCoordinates";
        public const string Exception = "Destination.GetAttractions.Exception";
    }
}
