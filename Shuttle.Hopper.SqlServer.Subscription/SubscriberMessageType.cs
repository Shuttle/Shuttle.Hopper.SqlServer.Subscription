namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriberMessageType
{
    public string InboxWorkQueueUri { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}