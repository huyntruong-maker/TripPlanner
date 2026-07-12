using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth.Commands.VerifyEmailCommand;

public record VerifyEmailCommand : ICommand<string>
{
    public string? Token { get; init; }
}

public class VerifyEmailCommandHandler(
    UserManager<User> userManager,
    IWriteUnitOfWork writeUnitOfWork) : IRequestHandler<VerifyEmailCommand, string>
{
    public async Task<string> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenRepo = writeUnitOfWork.GetRepository<EmailVerificationToken>();

        // The query filter in EmailVerificationTokenConfiguration excludes soft-deleted rows.
        var verificationToken = await tokenRepo.Single(t => t.Token == request.Token!);
        if (verificationToken == null)
        {
            return AuthControllerMsg.VerifyEmail.TokenInvalid;
        }

        if (verificationToken.ConsumedAt != null)
        {
            return AuthControllerMsg.VerifyEmail.AlreadyVerified;
        }

        if (verificationToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return AuthControllerMsg.VerifyEmail.TokenExpired;
        }

        var user = await userManager.FindByIdAsync(verificationToken.UserId.ToString());
        if (user == null)
        {
            return AuthControllerMsg.VerifyEmail.TokenInvalid;
        }

        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        verificationToken.ConsumedAt = DateTimeOffset.UtcNow;
        await tokenRepo.Update(verificationToken);
        await writeUnitOfWork.SaveChanges();

        return string.Empty;
    }
}
