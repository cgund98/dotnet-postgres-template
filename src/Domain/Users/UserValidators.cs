namespace PostgresTemplate.Domain.Users;

using PostgresTemplate.Domain.Exceptions;



public static class UserValidators
{
    const int MinAge = 1;
    const int MaxAge = 125;

    // Individual Field Validators

    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name cannot be empty", field: "name");
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email cannot be empty", field: "email");
    }

    public static void ValidateAge(int? age)
    {
        if (age is null)
            return;

        if (age < MinAge || age > MaxAge)
            throw new ValidationException($"Age must be between {MinAge} and {MaxAge}", field: "age");
    }

    // Command Validators

    public static async Task ValidateCreateUserAsync(CreateUserCommand command, IUserRepository userRepository)
    {
        ValidateName(command.Name);
        ValidateEmail(command.Email);
        ValidateAge(command.Age);

        var existing = await userRepository.GetByEmailAsync(command.Email);
        if (existing is not null)
            throw new DuplicateException("User", "email");
    }

    public static async Task ValidatePatchUserAsync(PatchUserCommand command, IUserRepository userRepository)
    {

        var user = await userRepository.GetByIdAsync(command.Id) ?? throw new NotFoundException("User", command.Id.ToString());

        if (command.Name is not null)
            ValidateName(command.Name);

        if (command.Email is not null)
            ValidateEmail(command.Email);

        if (command.Age is not null)
            ValidateAge(command.Age);
    }
}