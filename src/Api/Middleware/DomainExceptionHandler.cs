using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PostgresTemplate.Domain.Exceptions;
using Serilog;

namespace PostgresTemplate.Api.Middleware;

public class DomainExceptionHandler : IExceptionHandler
{
    private static ProblemDetails CreateProblem(int statusCode, string error, string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error,
            Detail = detail,
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        return problem;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var problem = exception switch
        {
            ValidationException ex => CreateProblem(
                StatusCodes.Status400BadRequest,
                "Validation Error",
                ex.Message,
                ex.Field is not null
                    ? new Dictionary<string, string[]> { [ex.Field] = [ex.Message] }
                    : null
            ),
            NotFoundException ex => CreateProblem(
                StatusCodes.Status404NotFound,
                "Not Found",
                ex.Message
            ),
            DuplicateException ex => CreateProblem(
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message
            ),
            BadHttpRequestException ex => CreateProblem(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                ex.Message
            ),
            _ => HandleInternalServerError(exception)
        };

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }

    private static ProblemDetails HandleInternalServerError(Exception exception)
    {
        Log.Error(exception, "An unexpected error occurred");
        return CreateProblem(
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred"
        );
    }
}