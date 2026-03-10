using System.Text.Json;

using PostgresTemplate.Domain.Exceptions;

namespace PostgresTemplate.Domain.Events;

public interface IEventHandlerRegistration
{
    Task HandleAsync(string data);
}

public class EventHandlerRegistration<TPayload>(IEventHandler<TPayload> eventHandler) : IEventHandlerRegistration
where TPayload : IEventPayload
{
    public Task HandleAsync(string data)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(data)
            ?? throw new EventEnvelopeDeserializationException(data);

        return eventHandler.HandleAsync(payload);
    }
}