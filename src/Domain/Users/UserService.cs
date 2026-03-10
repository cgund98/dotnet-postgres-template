namespace PostgresTemplate.Domain.Users;

using PostgresTemplate.Domain.Persistence;

public class UserService(IUserRepository userRepository, ITransactionManager transactionManager)
{
    // Queries

    public async Task<(List<User>, int)> ListAndCountUsersAsync(int limit, int offset)
    {
        var users = await userRepository.ListAsync(limit, offset);
        var total = await userRepository.CountAsync();
        return (users, total);
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }

    // Mutations

    public async Task<User> CreateUserAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        await UserValidators.ValidateCreateUserAsync(command, userRepository);
        return await transactionManager.TransactionAsync(async () => await userRepository.CreateAsync(command), ct);
    }

    public async Task<User> PatchUserAsync(PatchUserCommand command, CancellationToken ct = default)
    {
        await UserValidators.ValidatePatchUserAsync(command, userRepository);
        return await transactionManager.TransactionAsync(async () => await userRepository.UpdatePartialAsync(command), ct);
    }

    public async Task<User> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        return await transactionManager.TransactionAsync(async () => await userRepository.DeleteAsync(id), ct);
    }
}
