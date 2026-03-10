namespace PostgresTemplate.Domain.Events;


// EventEnvelope is a CloudEvents v1.0 compatible wrapper for events.
public record EventEnvelope(
    string Id,
    string Source,
    string Type,
    string Data, // Serialized JSON
    DateTimeOffset Time,
    Dictionary<string, string>? Extensions = null,
    string? Subject = null
)
{
    public static string SpecVersion => "1.0";
    public static string DataContentType => "application/json";

    // Extensions
    public const string AggregateIdExtension = "aggregate_id";
    public string AggregateId => Extensions?.GetValueOrDefault(AggregateIdExtension)
        ?? throw new InvalidOperationException("Aggregate ID is required on extensions");
}