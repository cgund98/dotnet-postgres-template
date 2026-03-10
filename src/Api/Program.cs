using FluentValidation;
using PostgresTemplate.Api.Common;
using PostgresTemplate.Api.Middleware;
using PostgresTemplate.Api.Users;
using PostgresTemplate.Domain.Persistence;
using PostgresTemplate.Domain.Users;
using PostgresTemplate.Infrastructure.Persistence.Postgres;
using PostgresTemplate.Infrastructure.Users;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration);

    if (context.HostingEnvironment.IsDevelopment())
        config.WriteTo.Console();
    else
        config.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
});

// Exception handlers
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Dapper: map snake_case columns to PascalCase properties
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Database
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Postgres")!
);

// Persistence (scoped = one per request, shared between TransactionManager and repositories)
builder.Services.AddScoped<DbContext>();
builder.Services.AddScoped<IDbContext>(sp => sp.GetRequiredService<DbContext>());
builder.Services.AddScoped<ITransactionManager, TransactionManager>();


// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<UserService>();

builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

// Map api endpoints under /api/v1
var api = app.MapGroup("/api/v1").AddEndpointFilter<ValidationFilter>();
api.MapUserEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();