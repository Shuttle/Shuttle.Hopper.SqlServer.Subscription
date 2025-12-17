using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Hopper.SqlServer.Subscription;

[Table("SubscriberMessageType")]
[PrimaryKey(nameof(InboxWorkQueueUri), nameof(MessageType))]
public class SubscriberMessageType
{
    [Required]
    [StringLength(250)]
    public string InboxWorkQueueUri { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string MessageType { get; set; } = string.Empty;
}