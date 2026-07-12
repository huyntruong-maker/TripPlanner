using MediatR;

namespace Application.Interfaces.Cqrs;

public interface IConsistentQuery<out TResponse> : IRequest<TResponse>;