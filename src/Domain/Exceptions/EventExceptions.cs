namespace PostgresTemplate.Domain.Exceptions;

public class EventException(string message) : Exception(message)
{
}

public class EventHandlerNotFoundException(string eventType) : EventException($"Event handler not found for event type: {eventType}")
{
    public string EventType { get; } = eventType;
}

public class EventEnvelopeDeserializationException(string errorMessage) : EventException($"Failed to deserialize event envelope: {errorMessage}")
{
    public string ErrorMessage { get; } = errorMessage;
}