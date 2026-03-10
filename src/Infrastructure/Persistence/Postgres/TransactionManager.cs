using Npgsql;
using PostgresTemplate.Domain.Persistence;

namespace PostgresTemplate.Infrastructure.Persistence.Postgres;

public class TransactionManager(NpgsqlDataSource dataSource, DbContext dbContext) : ITransactionManager
{
    public async Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        dbContext.SetConnection(connection);
        dbContext.Transaction = transaction;

        try
        {
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}