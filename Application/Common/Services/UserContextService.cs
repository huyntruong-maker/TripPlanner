using System.Security.Claims;
using Application.Dtos.Base;
using Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Application.Common.Services;

public interface IUserContextService
{
    CurrentUserContextDto GetCurrentUserContext();
}

public class UserContextService(IHttpContextAccessor httpContext) : IUserContextService
{
    public CurrentUserContextDto GetCurrentUserContext()
    {
        return new CurrentUserContextDto
        {
            UserId = Guid.TryParse(httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                        ? userId : Guid.Empty,
            RoleIds = httpContext.HttpContext?.User?.Claims
                        .Where(c => c.Type == UserConstants.RolesClaim)
                        .Select(c => Guid.TryParse(c.Value, out var roleId) ? roleId : (Guid?)null)
                        .Where(g => g.HasValue)
                        .Select(g => g!.Value)
                        .ToArray() ?? [],
            RolesLevel = httpContext.HttpContext?.User?.Claims
                        .Where(c => c.Type == UserConstants.RolesLevelClaim)
                        .Select(c => int.TryParse(c.Value, out var perm) ? perm : (int?)null)
                        .Where(p => p.HasValue)
                        .Select(p => p!.Value)
                        .ToArray() ?? []
        };
    }
}
