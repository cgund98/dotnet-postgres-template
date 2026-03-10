using System.Text.Json;
using PostgresTemplate.Domain.Exceptions;
using Serilog;

namespace PostgresTemplate.Domain.Events;



public class EventRouter
{
    private readonly Dictionary<string, IEventHandlerRegistration> _eventHandlers = [];

    public void Register<TPayload>(string eventType, IEventHandler<TPayload> eventHandler)
        where TPayload : IEventPayload
    {
        _eventHandlers.Add(eventType, new EventHandlerRegistration<TPayload>(eventHandler));
    }

    public async Task RouteStringAsync(string eventEnvelopeJson)
    {
        EventEnvelope eventEnvelope;
        try
        {
            eventEnvelope = JsonSerializer.Deserialize<EventEnvelope>(eventEnvelopeJson)
                ?? throw new EventEnvelopeDeserializationException(eventEnvelopeJson);
        }
        catch (JsonException ex)
        {
            throw new EventEnvelopeDeserializationException(ex.Message);
        }

        await RouteAsync(eventEnvelope);
    }

    public async Task RouteAsync(EventEnvelope eventEnvelope)
    {
        var handler = _eventHandlers.GetValueOrDefault(eventEnvelope.Type)
            ?? throw new EventHandlerNotFoundException(eventEnvelope.Type);

        Log.Information(
            "Routing event to handler: EventType={EventType} AggregateId={AggregateId} EventId={EventId}",
            eventEnvelope.Type, eventEnvelope.AggregateId, eventEnvelope.Id);

        try
        {
            await handler.HandleAsync(eventEnvelope.Data);
            Log.Information(
                "Event routed successfully: EventType={EventType} AggregateId={AggregateId} EventId={EventId}",
                eventEnvelope.Type, eventEnvelope.AggregateId, eventEnvelope.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error routing event to handler: EventType={EventType} AggregateId={AggregateId} EventId={EventId}",
                eventEnvelope.Type, eventEnvelope.AggregateId, eventEnvelope.Id);
            throw;
        }
    }
}