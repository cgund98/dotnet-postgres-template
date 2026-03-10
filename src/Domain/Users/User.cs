namespace PostgresTemplate.Domain.Users;

public record User(
    Guid Id,
    string Email,
    string Name,
    int? Age,
    DateTime CreatedAt,
    DateTime UpdatedAt
);