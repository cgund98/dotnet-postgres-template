using Amazon.SQS;
using Amazon.SQS.Model;
using PostgresTemplate.Domain.Events;
using Serilog;

namespace PostgresTemplate.Infrastructure.Events.Sqs;

public record SqsConsumerOptions(
    string QueueUrl,
    int MaxMessages = 10,
    int WaitTimeSeconds = 20,
    int BackoffDelayMilliseconds = 1000
);

public class SqsEventConsumer(IAmazonSQS sqsClient, EventRouter router, SqsConsumerOptions options)
    : IEventConsumer<string>
{
    public async Task ConsumeAsync(CancellationToken ct)
    {
        Log.Information("Starting SQS consumer: QueueUrl={QueueUrl}", options.QueueUrl);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PollAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error polling SQS: QueueUrl={QueueUrl}", options.QueueUrl);
                await Task.Delay(options.BackoffDelayMilliseconds, ct);
            }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MaxNumberOfMessages = options.MaxMessages,
            WaitTimeSeconds = options.WaitTimeSeconds,
        }, ct);

        if (response.Messages is null) return;

        foreach (var message in response.Messages)
        {
            try
            {
                await ProcessMessageAsync(message);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        try
        {
            await router.RouteStringAsync(message.Body);
            await AckAsync(message.ReceiptHandle);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process message: MessageId={MessageId} QueueUrl={QueueUrl}", message.MessageId, options.QueueUrl);
            try
            {
                await NackAsync(message.ReceiptHandle);
            }
            catch (Exception nackEx)
            {
                Log.Error(nackEx, "Failed to nack message: MessageId={MessageId} QueueUrl={QueueUrl}", message.MessageId, options.QueueUrl);
            }
        }
    }

    public async Task AckAsync(string receiptHandle)
    {
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = options.QueueUrl,
            ReceiptHandle = receiptHandle,
        });
    }

    public async Task NackAsync(string receiptHandle)
    {
        await sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = options.QueueUrl,
            ReceiptHandle = receiptHandle,
            VisibilityTimeout = 0,
        });
    }
}
