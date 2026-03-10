namespace PostgresTemplate.Api.Common;

public record PaginationParams(
    int Page = 1,
    int PageSize = 20
)
{
    public int Limit => PageSize;
    public int Offset => (Page - 1) * PageSize;
}

public record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int Total
)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
