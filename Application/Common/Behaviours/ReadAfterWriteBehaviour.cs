using Application.Common.Services;
using Application.Interfaces.Caching;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Common.Behaviours;

public class ReadAfterWriteBehaviour<TRequest, TResponse>(
    IReadUnitOfWork unitOfWork,
    ICacheManager cacheManager,
    IUserContextService userContextService,
    IConfiguration configuration) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Skip for normal queries and commands
        if (request is not IConsistentQuery<TResponse>)
        {
            return await next();
        }

        var userContext = userContextService.GetCurrentUserContext();
        if (!await cacheManager.GetData<bool>(string.Format(CacheKeys.ReadAfterWrite.UserWrote, userContext.UserId)))
        {
            return await next();
        }

        // Allow to read from the write database
        var writeDb = configuration.GetSection(ConfigKeys.Databases.WriteDatabase).Get<string>()!;
        unitOfWork.ChangeDatabase(writeDb);

        return await next();
    }
}