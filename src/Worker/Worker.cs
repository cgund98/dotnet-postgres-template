using PostgresTemplate.Infrastructure.Events.Sqs;
using Serilog;

namespace PostgresTemplate.Worker;

public class Worker(SqsEventConsumer consumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Starting worker");
        try
        {
            await consumer.ConsumeAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting worker");
            throw;
        }
    }
}