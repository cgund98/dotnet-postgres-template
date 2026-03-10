using System.Data;
using Npgsql;
using PostgresTemplate.Domain.Persistence;

namespace PostgresTemplate.Infrastructure.Persistence.Postgres;

public class DbContext(NpgsqlDataSource dataSource) : IDbContext
{

    private NpgsqlConnection? _connection;

    public IDbConnection Connection => _connection ??= dataSource.OpenConnection();
    public IDbTransaction? Transaction { get; set; }

    internal void SetConnection(NpgsqlConnection connection) => _connection = connection;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}