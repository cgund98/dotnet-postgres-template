using Dapper;
using PostgresTemplate.Domain.Persistence;
using PostgresTemplate.Domain.Users;

namespace PostgresTemplate.Infrastructure.Users;

public class UserRepository(IDbContext db) : IUserRepository
{
    public async Task<User> CreateAsync(CreateUserCommand command)
    {
        return await db.Connection.QueryFirstAsync<User>(
            """
            INSERT INTO users (id, email, name, age) 
            VALUES (@Id, @Email, @Name, @Age) 
            RETURNING id, email, name, age, created_at, updated_at
            """,
            new
            {
                Id = Guid.CreateVersion7(),
                command.Email,
                command.Name,
                command.Age
            },
            transaction: db.Transaction
        );
    }

    public async Task<User> UpdatePartialAsync(PatchUserCommand command)
    {
        return await db.Connection.QuerySingleAsync<User>(
            """
            UPDATE users
            SET email = COALESCE(@Email, email),
                name = COALESCE(@Name, name),
                age = COALESCE(@Age, age),
                updated_at = NOW()
            WHERE id = @Id
            RETURNING id, email, name, age, created_at, updated_at
            """,
            command,
            transaction: db.Transaction
        );
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await db.Connection.QuerySingleOrDefaultAsync<User>(
            """
            SELECT id, email, name, age, created_at, updated_at 
            FROM users
            WHERE id = @Id
            """,
            new { Id = id },
            transaction: db.Transaction
        );
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await db.Connection.QuerySingleOrDefaultAsync<User>(
            """
            SELECT id, email, name, age, created_at, updated_at 
            FROM users
            WHERE email = @Email
            """,
            new { Email = email },
            transaction: db.Transaction
        );
    }

    public async Task<User> DeleteAsync(Guid id)
    {
        return await db.Connection.QuerySingleAsync<User>(
            """
            DELETE FROM users
            WHERE id = @Id
            RETURNING id, email, name, age, created_at, updated_at
            """,
            new { Id = id },
            transaction: db.Transaction
        );
    }

    public async Task<List<User>> ListAsync(int limit, int offset)
    {
        var users = await db.Connection.QueryAsync<User>(
            """
            SELECT id, email, name, age, created_at, updated_at 
            FROM users
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset
            """,
            new { Limit = limit, Offset = offset },
            transaction: db.Transaction
        );
        return [.. users];
    }

    public async Task<int> CountAsync()
    {
        return await db.Connection.QuerySingleAsync<int>(
            """
            SELECT COUNT(*) FROM users
            """,
            transaction: db.Transaction
        );
    }
}