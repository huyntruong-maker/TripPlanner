using Application.Dtos.Hub;

namespace Application.Interfaces.Hub;

public interface IBroadcastMessageService
{
    Task SendMessageToAllAsync<T>(BroadcastMessage<T> message) where T : class;

    Task SendMessageToGroupAsync<T>(string groupName, BroadcastMessage<T> message) where T : class;
}