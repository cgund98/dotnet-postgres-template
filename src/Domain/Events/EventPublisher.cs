namespace PostgresTemplate.Domain.Events;

public record PublishMetadata(
    string Source
);

public interface IEventPublisher
{
    Task PublishAsync(IEventPayload payload, PublishMetadata metadata);
}