using MediatR;

namespace Application.Interfaces.Cqrs;

public interface IQuery<out TResponse> : IRequest<TResponse>;