namespace PostgresTemplate.Domain.Users;

using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Persistence;

public class UserService(IUserRepository userRepository, ITransactionManager transactionManager, IEventPublisher eventPublisher)
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
        var user = await transactionManager.TransactionAsync(async () => await userRepository.CreateAsync(command), ct);

        var userCreatedEvent = new UserCreatedEvent(user.Id, user.Email, user.Name, user.Age);
        await eventPublisher.PublishAsync(userCreatedEvent, new PublishMetadata("UserService.CreateUserAsync"));

        return user;
    }

    public async Task<User> PatchUserAsync(PatchUserCommand command, CancellationToken ct = default)
    {
        await UserValidators.ValidatePatchUserAsync(command, userRepository);
        var user = await transactionManager.TransactionAsync(async () => await userRepository.UpdatePartialAsync(command), ct);

        var userUpdatedEvent = new UserUpdatedEvent(user.Id, user.Email, user.Name, user.Age);
        await eventPublisher.PublishAsync(userUpdatedEvent, new PublishMetadata("UserService.PatchUserAsync"));

        return user;
    }

    public async Task<User> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await transactionManager.TransactionAsync(async () => await userRepository.DeleteAsync(id), ct)
            ?? throw new Exceptions.NotFoundException("User", id.ToString());

        var userDeletedEvent = new UserDeletedEvent(user.Id);
        await eventPublisher.PublishAsync(userDeletedEvent, new PublishMetadata("UserService.DeleteUserAsync"));

        return user;
    }
}
