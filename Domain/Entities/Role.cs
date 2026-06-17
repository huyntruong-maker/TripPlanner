using Domain.IEntities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Roles")]
public class Role : IdentityRole<Guid>, IBaseEntity, IIsDeletedEntity
{
    [MaxLength(256)] public string DisplayName { get; set; }

    public virtual ICollection<RoleClaim> RoleClaims { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int Level { get; set; }

    public bool IsDeleted { get; set; }
}