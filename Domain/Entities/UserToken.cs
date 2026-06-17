using Domain.IEntities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("UserTokens")]
public class UserToken : IdentityUserToken<Guid>, IBaseEntity
{
    public Guid DeviceUuid { get; set; }

    public required string RefreshToken { get; set; }

    public DateTimeOffset RefreshTokenExpiration { get; set; }

    public virtual User User { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool RememberMe { get; set; }

    [MaxLength(250)]
    public required string DeviceInfo { get; set; }

    [MaxLength(150)]
    public required string LocationInfo { get; set; }
}