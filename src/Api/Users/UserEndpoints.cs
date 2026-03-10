using PostgresTemplate.Api.Common;
using PostgresTemplate.Domain.Users;
using Serilog;

namespace PostgresTemplate.Api.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("users");

        // List users
        group.MapGet("/", async ([AsParameters] PaginationParams paginationParams, UserService userService) =>
        {
            Log
                .ForContext("PaginationParams", paginationParams)
                .Information("Listing users");

            var (users, total) = await userService.ListAndCountUsersAsync(paginationParams.Limit, paginationParams.Offset);

            var userResponses = users.Select(user => new UserResponse(
                Id: user.Id,
                Email: user.Email,
                Name: user.Name,
                Age: user.Age,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt
            )).ToList();

            return Results.Ok(new PaginatedResponse<UserResponse>(
                Items: userResponses,
                Page: paginationParams.Page,
                PageSize: paginationParams.PageSize,
                Total: total
            ));
        })
        .WithName("ListUsers")
        .WithDescription("List users with pagination")
        .Produces<PaginatedResponse<UserResponse>>(StatusCodes.Status200OK);

        // Get user by ID
        group.MapGet("/{id:guid}", async (Guid id, UserService userService) =>
        {
            Log.Information("Getting user by ID: Id={Id}", id);

            var user = await userService.GetUserByIdAsync(id);

            if (user is null)
                return Results.NotFound();

            var userResponse = new UserResponse(
                Id: user.Id,
                Email: user.Email,
                Name: user.Name,
                Age: user.Age,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt
            );

            return Results.Ok(userResponse);
        })
        .WithName("GetUser")
        .WithDescription("Get a user by ID")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        // Create user
        group.MapPost("/", async (CreateUserRequest request, UserService userService) =>
        {
            Log.Information("Creating user");

            var user = await userService.CreateUserAsync(new CreateUserCommand(
                Email: request.Email,
                Name: request.Name,
                Age: request.Age
            ));

            var userResponse = new UserResponse(
                Id: user.Id,
                Email: user.Email,
                Name: user.Name,
                Age: user.Age,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt
            );
            return Results.CreatedAtRoute("GetUser", new { id = user.Id }, userResponse);
        })
        .WithName("CreateUser")
        .WithDescription("Create a new user")
        .Produces<UserResponse>(StatusCodes.Status201Created);

        // Patch user
        group.MapPatch("/{id:guid}", async (Guid id, PatchUserRequest request, UserService userService) =>
        {
            Log.Information("Patching user by ID: Id={Id}", id);

            var user = await userService.PatchUserAsync(new PatchUserCommand(
                Id: id,
                Email: request.Email,
                Name: request.Name,
                Age: request.Age
            ));

            var userResponse = new UserResponse(
                Id: user.Id,
                Email: user.Email,
                Name: user.Name,
                Age: user.Age,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt
            );
            return Results.Ok(userResponse);
        })
        .WithName("PatchUser")
        .WithDescription("Patch a user by ID")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        // Delete user
        group.MapDelete("/{id:guid}", async (Guid id, UserService userService) =>
        {
            Log.Information("Deleting user by ID: Id={Id}", id);

            var user = await userService.DeleteUserAsync(id);

            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithDescription("Delete a user by ID")
        .Produces(StatusCodes.Status204NoContent);
    }
}