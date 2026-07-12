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

        return string.Empty;
    }
}