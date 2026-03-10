using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Users;
using Serilog;

namespace PostgresTemplate.Worker.Users;

public class UserUpdatedHandler : IEventHandler<UserUpdatedEvent>
{
    public Task HandleAsync(UserUpdatedEvent payload)
    {
        Log.Information(
            "Processing UserUpdatedEvent: AggregateId={AggregateId} Email={Email} Name={Name} Age={Age}",
            payload.AggregateId, payload.Email, payload.Name, payload.Age);

        return Task.CompletedTask;
    }
}
