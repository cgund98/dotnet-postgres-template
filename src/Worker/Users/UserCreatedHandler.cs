using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Users;
using Serilog;

namespace PostgresTemplate.Worker.Users;


public class UserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    public Task HandleAsync(UserCreatedEvent payload)
    {
        Log.Information(
            "Processing UserCreatedEvent: AggregateId={AggregateId} Email={Email} Name={Name} Age={Age}",
            payload.AggregateId, payload.Email, payload.Name, payload.Age);

        return Task.CompletedTask;
    }
}