using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class SecurityInjection
{
    public static void AddSecurity(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbIdentity(configuration);
        collection.AddJwtBearer(configuration);
        collection.AddClaimsBasedAuthorization();
    }
}