namespace WebApi.Models.Requests.Trip;

public class SetTripDatesReq
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
