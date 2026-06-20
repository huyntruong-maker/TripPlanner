using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.IEntities;

namespace Domain.Entities;

[Table("EmailVerificationTokens")]
public class EmailVerificationToken : BaseEntity, IIsDeletedEntity
{
    public Guid UserId { get; set; }

    [MaxLength(512)]
    public required string Token { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User User { get; set; } = null!;
}
