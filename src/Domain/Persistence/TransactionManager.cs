namespace PostgresTemplate.Domain.Persistence;

public interface ITransactionManager
{
    Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}