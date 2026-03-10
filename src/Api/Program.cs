using Amazon.SimpleNotificationService;
using FluentValidation;
using PostgresTemplate.Api.Common;
using PostgresTemplate.Api.Middleware;
using PostgresTemplate.Api.Users;
using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Persistence;
using PostgresTemplate.Domain.Users;
using PostgresTemplate.Infrastructure.Events.Sns;
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

// AWS
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
{
    var config = new AmazonSimpleNotificationServiceConfig();
    if (builder.Environment.IsDevelopment())
    {
        config.ServiceURL = "http://localstack:4566";
    }
    return new AmazonSimpleNotificationServiceClient(config);
});

// Dapper: map snake_case columns to PascalCase properties
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Database
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Postgres")!
);

// Events
builder.Services.AddScoped<IEventPublisher>(sp =>
    new SnsEventPublisher(
        sp.GetRequiredService<IAmazonSimpleNotificationService>(),
        builder.Configuration["Aws:EventTopicArn"]
            ?? throw new InvalidOperationException("Event topic ARN is not configured")
    )
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