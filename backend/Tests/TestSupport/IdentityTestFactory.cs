using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.TestSupport;

public static class IdentityTestFactory
{
    public static UserManager<User> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<User>>();

        return Substitute.For<UserManager<User>>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            NullLogger<UserManager<User>>.Instance);
    }

    public static SignInManager<User> CreateSignInManager(UserManager<User> userManager)
    {
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<User>>();
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        var confirmation = Substitute.For<IUserConfirmation<User>>();

        return Substitute.For<SignInManager<User>>(
            userManager,
            contextAccessor,
            claimsFactory,
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<User>>.Instance,
            schemes,
            confirmation);
    }
}
