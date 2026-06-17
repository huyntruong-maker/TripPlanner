using Application.Dtos.Base;
using Domain.Messages;

namespace Application.Common.Validators;

public class UserContextValidator
{
    public static string ValidateUserContext(CurrentUserContextDto userContext)
    {
        if (userContext.UserId == Guid.Empty)
        {
            return ShareControllerMsg.CurrentUserContext.InvalidUser;
        }

        if (userContext.RoleIds.Length == 0 || userContext.RolesLevel.Length == 0)
        {
            return ShareControllerMsg.CurrentUserContext.InvalidRole;
        }

        return string.Empty;
    }
}