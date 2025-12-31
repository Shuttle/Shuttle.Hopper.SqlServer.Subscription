using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using System.Diagnostics.CodeAnalysis;

namespace Shuttle.Hopper.SqlServer.Subscription;

[SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection", Justification = "Schema and table names are from trusted configuration sources")]
public class SubscriptionObserver(IOptions<HopperOptions> hopperOptions, IOptions<SqlServerSubscriptionOptions> sqlSubscriptionOptions, IDbContextFactory<SqlServerSubscriptionDbContext> dbContextFactory)
    : IPipelineObserver<Started>
{
    private readonly IDbContextFactory<SqlServerSubscriptionDbContext> _dbContextFactory = Guard.AgainstNull(dbContextFactory);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly SqlServerSubscriptionOptions _sqlServerSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<Started> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_hopperOptions.Inbox.WorkTransportUri == null)
        {
            throw new InvalidOperationException(Hopper.Resources.SubscribeWithNoInboxException);
        }

        var messageTypes = _hopperOptions.Subscription.MessageTypes;

        if (!messageTypes.Any() || _hopperOptions.Subscription.Mode == SubscriptionMode.Disabled)
        {
            return;
        }

        if (!_sqlServerSubscriptionOptions.ConfigureDatabase)
        {
            return;
        }

        var inboxWorkQueueUri = _hopperOptions.Inbox.WorkTransportUri.ToString();
        var missingMessageTypes = new List<string>();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        await dbContext.Database.ExecuteSqlRawAsync($@"
DECLARE @lock_result INT;
EXEC @lock_result = sp_getapplock @Resource = '{typeof(SubscriptionObserver).FullName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 15000;

BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_sqlServerSubscriptionOptions.Schema}')
    BEGIN
        EXEC('CREATE SCHEMA {_sqlServerSubscriptionOptions.Schema}');
    END

    IF OBJECT_ID ('{_sqlServerSubscriptionOptions.Schema}.SubscriberMessageType', 'U') IS NULL 
    BEGIN
        CREATE TABLE [{_sqlServerSubscriptionOptions.Schema}].[SubscriberMessageType]
        (
            [MessageType] [varchar](250) NOT NULL,
            [InboxWorkQueueUri] [varchar](250) NOT NULL,
            CONSTRAINT [PK_SubscriberMessageType] PRIMARY KEY CLUSTERED ([MessageType] ASC, [InboxWorkQueueUri] ASC)
        )
    END
END TRY
BEGIN CATCH
    EXEC sp_releaseapplock @Resource = '{typeof(SubscriptionObserver).FullName}', @LockOwner = 'Session';
    THROW;
END CATCH

EXEC sp_releaseapplock @Resource = '{typeof(SubscriptionObserver).FullName}', @LockOwner = 'Session';
", cancellationToken);

        foreach (var messageType in messageTypes)
        {
            var isSubscribed = await dbContext.Database
                .SqlQueryRaw<int>($@"
SELECT 
    COUNT(1) AS [Value] 
FROM 
    [{_sqlServerSubscriptionOptions.Schema}].[SubscriberMessageType] 
WHERE 
    MessageType = '{messageType}' AND InboxWorkQueueUri = '{inboxWorkQueueUri}'")
                .FirstOrDefaultAsync(cancellationToken) > 0;

            if (isSubscribed)
            {
                continue;
            }

            switch (_hopperOptions.Subscription.Mode)
            {
                case SubscriptionMode.Standard:
                {
                    await dbContext.Database.ExecuteSqlRawAsync($@"
INSERT INTO [{_sqlServerSubscriptionOptions.Schema}].[SubscriberMessageType]
(
    MessageType, 
    InboxWorkQueueUri
)
VALUES 
(
    '{messageType}', 
    '{inboxWorkQueueUri}'
)", cancellationToken);
                    break;
                }
                case SubscriptionMode.FailWhenMissing:
                {
                    missingMessageTypes.Add(messageType);
                    break;
                }
            }
        }

        if (missingMessageTypes.Any())
        {
            throw new ApplicationException(string.Format(Resources.MissingSubscriptionException, string.Join(",", missingMessageTypes)));
        }
    }
}