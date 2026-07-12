using Application.Interfaces.Email;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Email;

public static class EmailInjection
{
    public static void AddEmail(this IServiceCollection collection)
    {
        collection.AddScoped<IEmailService, EmailService>();
    }
}