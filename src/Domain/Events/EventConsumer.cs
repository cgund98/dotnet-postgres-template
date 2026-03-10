namespace PostgresTemplate.Domain.Events;

public interface IEventConsumer<TMessageId>
{
    Task ConsumeAsync(CancellationToken ct);
    Task AckAsync(TMessageId messageId);
    Task NackAsync(TMessageId messageId);
}