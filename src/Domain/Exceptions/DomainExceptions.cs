namespace PostgresTemplate.Domain.Exceptions;

public class DomainException(string message) : Exception(message)
{
}

public class ValidationException(string message, string? field = null) : DomainException(message)
{
    public string? Field { get; } = field;
}

public class NotFoundException(string entityType, string id)
    : DomainException($"{entityType} not found with id: {id}")
{
    public string EntityType { get; } = entityType;
    public string Id { get; } = id;
}

public class DuplicateException(string entityType, string identifier)
    : DomainException($"{entityType} already exists with identifier: {identifier}")
{
    public string EntityType => entityType;
    public string Identifier => identifier;
}
