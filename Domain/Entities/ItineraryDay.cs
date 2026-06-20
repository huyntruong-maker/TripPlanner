using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("ItineraryDays")]
public class ItineraryDay
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public DateOnly Date { get; set; }

    public int DayIndex { get; set; }

    public virtual Trip Trip { get; set; } = null!;

    public virtual ICollection<TripDestination> TripDestinations { get; set; } = [];
}
