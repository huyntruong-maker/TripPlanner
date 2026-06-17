using Application.Features.Auth.Shared;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Application.Features.Auth.Commands.LogoutCommand;

public record LogoutCommand : ICommand<bool>
{
    public required string Token { get; set; }

    public required string RefreshToken { get; set; }

    public Guid DeviceUuid { get; set; }
}

public class LogoutCommandHandler(
    ILogger<LogoutCommandHandler> logger,
    IAuthShareService authShareService,
    IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await authShareService.VerifyToken(request.Token, authShareService.Secret);
            if (user == null) return false;

            user = await authShareService.VerifyUserToken(request.DeviceUuid, request.Token, request.RefreshToken);
            if (user == null) return false;

            var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
            var userToken = (await userTokenRepo.QueryCondition(i => i.UserId == user.Id
                                                                     && i.Value == request.Token
                                                                     && i.RefreshToken == request.RefreshToken))
                .First();

            await userTokenRepo.Delete([userToken.UserId, userToken.LoginProvider, userToken.Name]);
            await writeUnitOfWork.SaveChanges();

            return true;
        }
        catch (SecurityTokenValidationException ex)
        {
            logger.LogError("Token is not valid {ex}", ex);
            return false;
        }
    }
}