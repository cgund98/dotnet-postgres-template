using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using PostgresTemplate.Domain.Events;
using Serilog;

namespace PostgresTemplate.Infrastructure.Events.Sns;

public class SnsEventPublisher(IAmazonSimpleNotificationService snsClient, string topicArn)
: IEventPublisher
{
    public async Task PublishAsync(IEventPayload payload, PublishMetadata metadata)
    {
        var envelope = payload.ToEnvelope(metadata.Source);

        Log.Information(
            "Publishing event to SNS: TopicArn={TopicArn} EventType={EventType} AggregateId={AggregateId}",
            topicArn, envelope.Type, envelope.AggregateId);

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = JsonSerializer.Serialize(envelope),
            MessageGroupId = envelope.AggregateId,
            MessageDeduplicationId = envelope.Id,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "event_type", new MessageAttributeValue { DataType = "String", StringValue = envelope.Type } },
            },
        };

        await snsClient.PublishAsync(request);
    }
}