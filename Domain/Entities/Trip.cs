using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.IEntities;

namespace Domain.Entities;

[Table("Trips")]
public class Trip : BaseEntity, IIsDeletedEntity
{
    public Guid UserId { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<ItineraryDay> ItineraryDays { get; set; } = [];

    public virtual ICollection<TripDestination> TripDestinations { get; set; } = [];
}
