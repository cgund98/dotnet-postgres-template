using FluentValidation;

namespace PostgresTemplate.Api.Users;

public record CreateUserRequest(
    string Email,
    string Name,
    int? Age
);

public record PatchUserRequest(
    string? Email,
    string? Name,
    int? Age
);

public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    int? Age,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Age).InclusiveBetween(0, 125).When(x => x.Age is not null);
    }
}

public class PatchUserRequestValidator : AbstractValidator<PatchUserRequest>
{
    public PatchUserRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress().NotEmpty().When(x => x.Email is not null);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Age).InclusiveBetween(0, 125).When(x => x.Age is not null);
    }
}