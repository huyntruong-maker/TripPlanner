using Domain.IEntities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Users")]
public class User : IdentityUser<Guid>, IBaseEntity, IIsDeletedEntity
{
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(500)]
    public string? ResetPasswordToken { get; set; }

    public DateTimeOffset? ResetPasswordExpiration { get; set; }

    public virtual ICollection<UserClaim> UserClaims { get; set; }

    public virtual ICollection<UserLogin> UserLogins { get; set; }

    public virtual ICollection<UserToken> UserTokens { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}