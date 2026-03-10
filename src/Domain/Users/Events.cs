using PostgresTemplate.Domain.Events;

namespace PostgresTemplate.Domain.Users;

public static class UserEventTypes
{
    public const string Created = "users.v1.created";
    public const string Updated = "users.v1.updated";
    public const string Deleted = "users.v1.deleted";
}

public record UserCreatedEvent(
    Guid Id,
    string Email,
    string Name,
    int? Age
) : IEventPayload
{
    public string AggregateId => Id.ToString();
    public string EventType => UserEventTypes.Created;
}

public record UserUpdatedEvent(
    Guid Id,
    string Email,
    string Name,
    int? Age
) : IEventPayload
{
    public string AggregateId => Id.ToString();
    public string EventType => UserEventTypes.Updated;
}

public record UserDeletedEvent(
    Guid Id
) : IEventPayload
{
    public string AggregateId => Id.ToString();
    public string EventType => UserEventTypes.Deleted;
}