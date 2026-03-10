namespace PostgresTemplate.Domain.Events;

public interface IEventHandler<TEvent> where TEvent : IEventPayload
{
    Task HandleAsync(TEvent payload);
}
