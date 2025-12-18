using Microsoft.EntityFrameworkCore;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriptionQuery(ISqlServerSubscriptionDbContextFactory dbContextFactory) : ISubscriptionQuery
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly SqlServerSubscriptionDbContext _dbContext = Guard.AgainstNull(dbContextFactory).Create();

    public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(messageType);

        return await _dbContext.SubscriberMessageTypes
            .Where(item => item.MessageType == messageType)
            .Select(item => item.InboxWorkQueueUri)
            .ToListAsync(cancellationToken);
    }
}