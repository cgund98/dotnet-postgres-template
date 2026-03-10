using System.Text.Json;

namespace PostgresTemplate.Domain.Events;

public interface IEventPayload
{
    string AggregateId { get; }
    string EventType { get; }
}

public static class EventPayloadExtensions
{
    public static EventEnvelope ToEnvelope(this IEventPayload payload, string source) => new(
        Id: Guid.NewGuid().ToString(),
        Source: source,
        Type: payload.EventType,
        Data: JsonSerializer.Serialize(payload, payload.GetType()),
        Time: DateTimeOffset.UtcNow,
        Extensions: new Dictionary<string, string>
        {
            [EventEnvelope.AggregateIdExtension] = payload.AggregateId
        },
        Subject: payload.AggregateId
    );
}