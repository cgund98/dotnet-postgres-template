#!/bin/bash
# Setup script for LocalStack SNS FIFO topics and SQS FIFO queues

set -e

ENDPOINT_URL="${AWS_ENDPOINT_URL:-http://localhost:4566}"
REGION="${AWS_REGION:-us-east-1}"

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
export AWS_DEFAULT_REGION="${REGION}"

echo "Setting up LocalStack resources..."
echo "Endpoint: $ENDPOINT_URL"
echo "Region: $REGION"
echo ""

# --- SNS FIFO Topic ---

echo "Creating SNS FIFO topic..."
TOPIC_ARN=$(aws --endpoint-url=$ENDPOINT_URL sns create-topic \
  --name events.fifo \
  --attributes '{"FifoTopic":"true","ContentBasedDeduplication":"false"}' \
  --region $REGION \
  --output text \
  --query 'TopicArn' 2>&1) || {
  echo "Error creating SNS FIFO topic:" >&2
  echo "$TOPIC_ARN" >&2
  exit 1
}

if [ -z "$TOPIC_ARN" ] || [[ ! "$TOPIC_ARN" =~ ^arn: ]]; then
  echo "Error: Failed to create SNS FIFO topic. Got: $TOPIC_ARN" >&2
  exit 1
fi

echo "Created SNS FIFO topic: $TOPIC_ARN"

# --- SQS FIFO Queues ---

create_fifo_queue() {
  local name=$1
  local queue_url

  echo "Creating $name.fifo queue..." >&2
  queue_url=$(aws --endpoint-url=$ENDPOINT_URL sqs create-queue \
    --queue-name "$name.fifo" \
    --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"false"}' \
    --region $REGION \
    --output text \
    --query 'QueueUrl' 2>&1) || {
    echo "Error creating $name.fifo queue:" >&2
    echo "$queue_url" >&2
    exit 1
  }

  echo "Created queue: $queue_url" >&2
  echo "$queue_url"
}

get_queue_arn() {
  local queue_url=$1
  local queue_arn

  queue_arn=$(aws --endpoint-url=$ENDPOINT_URL sqs get-queue-attributes \
    --queue-url "$queue_url" \
    --attribute-names QueueArn \
    --region $REGION \
    --output text \
    --query 'Attributes.QueueArn' 2>&1) || {
    echo "Error getting queue ARN for $queue_url:" >&2
    echo "$queue_arn" >&2
    exit 1
  }

  echo "$queue_arn"
}

subscribe_queue() {
  local queue_arn=$1
  local filter_policy=$2
  local label=$3

  echo "Subscribing $label..."
  aws --endpoint-url=$ENDPOINT_URL sns subscribe \
    --topic-arn "$TOPIC_ARN" \
    --protocol sqs \
    --notification-endpoint "$queue_arn" \
    --attributes "{\"FilterPolicy\":\"$filter_policy\",\"RawMessageDelivery\":\"true\"}" \
    --region $REGION \
    --output text \
    --query 'SubscriptionArn' > /dev/null 2>&1 || {
    echo "Error subscribing $label:" >&2
    exit 1
  }

  echo "Subscribed $label with filter policy and raw message delivery"
}

# Create queues
USER_EVENTS_QUEUE=$(create_fifo_queue "user-events")

# Get queue ARNs
USER_EVENTS_QUEUE_ARN=$(get_queue_arn "$USER_EVENTS_QUEUE")

# Subscribe with filter policies (one queue per domain, multiple event types)
subscribe_queue "$USER_EVENTS_QUEUE_ARN" \
  '{\"event_type\":[\"users.v1.created\",\"users.v1.updated\",\"users.v1.deleted\"]}' \
  "user-events queue"

echo ""
echo "Setup complete!"
echo ""
echo "AWS_ENDPOINT_URL=$ENDPOINT_URL"
echo "AWS_REGION=$REGION"
echo "DEFAULT_EVENT_TOPIC_ARN=$TOPIC_ARN"
echo "EVENT_QUEUE_URL_USER_EVENTS=$USER_EVENTS_QUEUE"
