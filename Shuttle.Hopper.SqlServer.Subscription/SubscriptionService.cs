using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriptionService(IOptions<SqlServerSubscriptionOptions> sqlSubscriptionOptions, ISqlServerSubscriptionDbContextFactory dbContextFactory)
    : ISubscriptionService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly SqlServerSubscriptionOptions _sqlServerSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);
    private readonly IMemoryCache _subscribersCache = new MemoryCache(new MemoryCacheOptions());
    private readonly SqlServerSubscriptionDbContext _dbContext = Guard.AgainstNull(dbContextFactory).Create();

    public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(messageType);

        await Lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (!_subscribersCache.TryGetValue(messageType, out IEnumerable<string>? subscribers))
            {
                subscribers = await _dbContext.SubscriberMessageTypes
                    .Where(item => item.MessageType == messageType)
                    .Select(item => item.InboxWorkQueueUri)
                    .ToListAsync(cancellationToken: cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_sqlServerSubscriptionOptions.CacheTimeout);

                _subscribersCache.Set(messageType, subscribers, cacheEntryOptions);
            }

            return subscribers ?? [];
        }
        finally
        {
            Lock.Release();
        }
    }
}