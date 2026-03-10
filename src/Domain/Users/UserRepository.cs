namespace PostgresTemplate.Domain.Users;

public interface IUserRepository
{
    Task<User> CreateAsync(CreateUserCommand command);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> UpdatePartialAsync(PatchUserCommand command);
    Task<User?> DeleteAsync(Guid id);
    Task<List<User>> ListAsync(int limit, int offset);
    Task<int> CountAsync();
}