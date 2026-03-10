using System.Data;

namespace PostgresTemplate.Domain.Persistence;

public interface IDbContext
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}