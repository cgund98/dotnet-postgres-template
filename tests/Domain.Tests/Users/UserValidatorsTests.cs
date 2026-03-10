using NSubstitute;
using PostgresTemplate.Domain.Exceptions;
using PostgresTemplate.Domain.Users;

namespace PostgresTemplate.Domain.Tests.Users;

public class UserValidatorsTests
{
    // -- Validate Name --
    [Fact]
    public void ValidateName_WithValidName_DoesNotThrow()
    {
        UserValidators.ValidateName("Test User");
    }

    [Fact]
    public void ValidateName_WithEmptyName_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateName(""));
    }

    [Fact]
    public void ValidateName_WithWhitespaceName_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateName("   "));
    }

    // -- Validate Email --
    [Fact]
    public void ValidateEmail_WithValidEmail_DoesNotThrow()
    {
        UserValidators.ValidateEmail("test@example.com");
    }

    [Fact]
    public void ValidateEmail_WithEmptyEmail_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateEmail(""));
    }

    [Fact]
    public void ValidateEmail_WithWhitespaceEmail_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateEmail("   "));
    }

    // -- Validate Age --
    [Fact]
    public void ValidateAge_WithValidAge_DoesNotThrow()
    {
        UserValidators.ValidateAge(20);
    }

    [Fact]
    public void ValidateAge_WithNullAge_DoesNotThrow()
    {
        UserValidators.ValidateAge(null);
    }

    [Fact]
    public void ValidateAge_WithNegativeAge_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateAge(-1));
    }

    [Fact]
    public void ValidateAge_WithInvalidAge_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => UserValidators.ValidateAge(1000));
    }

    // -- Validate Create User --
    [Fact]
    public async Task ValidateCreateUserAsync_WithValidCommand_DoesNotThrow()
    {

        var repo = Substitute.For<IUserRepository>();
        repo.GetByEmailAsync("test@example.com")
        .Returns(Task.FromResult<User?>(null));

        var command = new CreateUserCommand(Email: "test@example.com", Name: "Test User", Age: 20);

        await UserValidators.ValidateCreateUserAsync(command, repo);
    }

    [Fact]
    public async Task ValidateCreateUserAsync_WithEmptyEmail_ThrowsValidationException()
    {
        var repo = Substitute.For<IUserRepository>();
        var command = new CreateUserCommand(Email: "", Name: "Test User", Age: 20);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidateCreateUserAsync(command, repo));
    }

    [Fact]
    public async Task ValidateCreateUserAsync_WithEmptyName_ThrowsValidationException()
    {
        var repo = Substitute.For<IUserRepository>();
        var command = new CreateUserCommand(Email: "test@example.com", Name: "", Age: 20);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidateCreateUserAsync(command, repo));
    }

    [Fact]
    public async Task ValidateCreateUserAsync_WithInvalidAge_ThrowsValidationException()
    {
        var repo = Substitute.For<IUserRepository>();
        var command = new CreateUserCommand(Email: "test@example.com", Name: "Test User", Age: -1);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidateCreateUserAsync(command, repo));
    }

    [Fact]
    public async Task ValidateCreateUserAsync_WithDuplicateEmail_ThrowsDuplicateException()
    {
        var user = new User(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20, CreatedAt: DateTime.UtcNow, UpdatedAt: DateTime.UtcNow);

        var repo = Substitute.For<IUserRepository>();
        repo.GetByEmailAsync("test@example.com")
        .Returns(Task.FromResult<User?>(user));

        var command = new CreateUserCommand(Email: "test@example.com", Name: "Test User", Age: 20);

        await Assert.ThrowsAsync<DuplicateException>(() => UserValidators.ValidateCreateUserAsync(command, repo));
    }

    // -- Validate Patch User --
    [Fact]
    public async Task ValidatePatchUserAsync_WithValidCommand_DoesNotThrow()
    {
        var user = new User(
            Id: Guid.NewGuid(),
            Email: "test@example.com",
            Name: "Test User", Age: 20,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        var repo = Substitute.For<IUserRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult<User?>(user));

        var command = new PatchUserCommand(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20);

        await UserValidators.ValidatePatchUserAsync(command, repo);
    }

    [Fact]
    public async Task ValidatePatchUserAsync_WithNullFields_DoesNotThrow()
    {
        var user = new User(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20, CreatedAt: DateTime.UtcNow, UpdatedAt: DateTime.UtcNow);

        var repo = Substitute.For<IUserRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult<User?>(user));

        var command = new PatchUserCommand(Id: Guid.NewGuid());

        await UserValidators.ValidatePatchUserAsync(command, repo);
    }

    [Fact]
    public async Task ValidatePatchUserAsync_WithEmptyName_ThrowsValidationException()
    {
        var user = new User(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20, CreatedAt: DateTime.UtcNow, UpdatedAt: DateTime.UtcNow);

        var repo = Substitute.For<IUserRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult<User?>(user));

        var command = new PatchUserCommand(Id: Guid.NewGuid(), Email: "test@example.com", Name: "", Age: 20);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidatePatchUserAsync(command, repo));
    }

    [Fact]
    public async Task ValidatePatchUserAsync_WithEmptyEmail_ThrowsValidationException()
    {
        var user = new User(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20, CreatedAt: DateTime.UtcNow, UpdatedAt: DateTime.UtcNow);

        var repo = Substitute.For<IUserRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult<User?>(user));

        var command = new PatchUserCommand(Id: Guid.NewGuid(), Email: "", Name: "Test User", Age: 20);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidatePatchUserAsync(command, repo));
    }

    [Fact]
    public async Task ValidatePatchUserAsync_WithInvalidAge_ThrowsValidationException()
    {
        var user = new User(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: 20, CreatedAt: DateTime.UtcNow, UpdatedAt: DateTime.UtcNow);

        var repo = Substitute.For<IUserRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult<User?>(user));

        var command = new PatchUserCommand(Id: Guid.NewGuid(), Email: "test@example.com", Name: "Test User", Age: -1);

        await Assert.ThrowsAsync<ValidationException>(() => UserValidators.ValidatePatchUserAsync(command, repo));
    }
}