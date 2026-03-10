namespace PostgresTemplate.Domain.Users;

public record CreateUserCommand(
    string Email,
    string Name,
    int? Age = null
);

public record PatchUserCommand(
    Guid Id,
    string? Email = null,
    string? Name = null,
    int? Age = null
);
