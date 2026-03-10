using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Users;
using Serilog;

namespace PostgresTemplate.Worker.Users;

public class UserDeletedHandler : IEventHandler<UserDeletedEvent>
{
    public Task HandleAsync(UserDeletedEvent payload)
    {
        Log.Information(
            "Processing UserDeletedEvent: AggregateId={AggregateId}",
            payload.AggregateId);

        return Task.CompletedTask;
    }
}
