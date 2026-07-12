using System.Security.Claims;
using Application.Dtos.Base;
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
                        ? userId : Guid.Empty
        };
    }
}
