using Domain.IEntities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("UserLogins")]
public class UserLogin : IdentityUserLogin<Guid>, IBaseEntity, IIsDeletedEntity
{
    public virtual User User { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}