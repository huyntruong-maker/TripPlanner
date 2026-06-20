using System.ComponentModel.DataAnnotations.Schema;
using Domain.IEntities;

namespace Domain.Entities;

[Table("ItineraryDays")]
public class ItineraryDay : BaseEntity, IIsDeletedEntity
{
    public Guid TripId { get; set; }

    public DateOnly Date { get; set; }

    public int DayIndex { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Trip Trip { get; set; } = null!;

    public virtual ICollection<TripDestination> TripDestinations { get; set; } = [];
}
