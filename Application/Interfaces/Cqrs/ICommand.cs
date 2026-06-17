using MediatR;

namespace Application.Interfaces.Cqrs;

public interface ICommand<out TResponse> : IRequest<TResponse>;