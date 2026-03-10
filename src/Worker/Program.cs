using Amazon.SQS;
using PostgresTemplate.Domain.Events;
using PostgresTemplate.Domain.Users;
using PostgresTemplate.Infrastructure.Events.Sqs;
using PostgresTemplate.Worker;
using PostgresTemplate.Worker.Users;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config =>
{
    if (builder.Environment.IsDevelopment())
        config.WriteTo.Console();
    else
        config.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
});

// SQS client
builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var config = new AmazonSQSConfig();

    if (builder.Environment.IsDevelopment())
    {
        config.ServiceURL = "http://localstack:4566";
    }

    return new AmazonSQSClient(config);
});

// Event router
builder.Services.AddSingleton(sp =>
{
    var router = new EventRouter();
    router.Register(UserEventTypes.Created, new UserCreatedHandler());
    router.Register(UserEventTypes.Updated, new UserUpdatedHandler());
    router.Register(UserEventTypes.Deleted, new UserDeletedHandler());
    return router;
});

// SQS consumer
builder.Services.AddSingleton(new SqsConsumerOptions(
    QueueUrl: builder.Configuration["Aws:UserEventsQueueUrl"]
        ?? throw new InvalidOperationException("User events queue URL is not configured"),
    MaxMessages: builder.Configuration.GetValue<int?>("Aws:UserEventsQueueMaxMessages") ?? 1,
    WaitTimeSeconds: builder.Configuration.GetValue<int?>("Aws:UserEventsQueueWaitTimeSeconds") ?? 5,
    BackoffDelayMilliseconds: builder.Configuration.GetValue<int?>("Aws:UserEventsQueueBackoffDelayMilliseconds") ?? 1000
));
builder.Services.AddSingleton<SqsEventConsumer>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();