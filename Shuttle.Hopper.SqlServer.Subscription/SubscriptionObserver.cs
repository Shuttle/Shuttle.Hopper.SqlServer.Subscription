using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriptionObserver(IOptions<ServiceBusOptions> serviceBusOptions, IOptions<SqlServerSubscriptionOptions> sqlSubscriptionOptions, ISqlServerSubscriptionDbContextFactory dbContextFactory)
    : IPipelineObserver<Started>
{
    private readonly SqlServerSubscriptionDbContext _dbContext = Guard.AgainstNull(dbContextFactory).Create();
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private readonly SqlServerSubscriptionOptions _sqlServerSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<Started> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_serviceBusOptions.Inbox.WorkTransportUri == null)
        {
            throw new InvalidOperationException(Hopper.Resources.SubscribeWithNoInboxException);
        }

        var messageTypes = _serviceBusOptions.Subscription.MessageTypes;

        if (!messageTypes.Any() ||
            _serviceBusOptions.Subscription.Mode == SubscriptionMode.Disabled)
        {
            return;
        }

        var missingMessageTypes = new List<string>();

        if (_sqlServerSubscriptionOptions.ConfigureDatabase)
        {
            await _dbContext.Database.ExecuteSqlRawAsync($@"
EXEC sp_getapplock @Resource = '{typeof(SubscriptionObserver).FullName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 15000;

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
        CONSTRAINT 
            [PK_SubscriberMessageType] 
        PRIMARY KEY CLUSTERED 
        (
	        [MessageType] ASC,
	        [InboxWorkQueueUri] ASC
        )
        WITH 
        (
            PAD_INDEX = OFF, 
            STATISTICS_NORECOMPUTE = OFF, 
            IGNORE_DUP_KEY = OFF, 
            ALLOW_ROW_LOCKS = ON, 
            ALLOW_PAGE_LOCKS = ON
        ) 
        ON [PRIMARY]
    ) 
    ON [PRIMARY]
END

EXEC sp_releaseapplock @Resource = '{typeof(SubscriptionObserver).FullName}', @LockOwner = 'Session';
", cancellationToken);
        }

        var inboxWorkQueueUri = _serviceBusOptions.Inbox.WorkTransportUri.ToString();

        foreach (var messageType in messageTypes)
        {
            switch (_serviceBusOptions.Subscription.Mode)
            {
                case SubscriptionMode.Standard:
                {
                    if (await _dbContext.SubscriberMessageTypes.FirstOrDefaultAsync(item => item.MessageType == messageType && item.InboxWorkQueueUri == inboxWorkQueueUri, cancellationToken) == null)
                    {
                        _dbContext.SubscriberMessageTypes.Add(new()
                        {
                            MessageType = messageType,
                            InboxWorkQueueUri = inboxWorkQueueUri
                        });
                    }

                    break;
                }
                case SubscriptionMode.FailWhenMissing:
                {
                    if (await _dbContext.SubscriberMessageTypes.FirstOrDefaultAsync(item => item.MessageType == messageType && item.InboxWorkQueueUri == inboxWorkQueueUri, cancellationToken) == null)
                    {
                        missingMessageTypes.Add(messageType);
                    }

                    break;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!missingMessageTypes.Any())
        {
            return;
        }

        throw new ApplicationException(string.Format(Resources.MissingSubscriptionException, string.Join(",", missingMessageTypes)));
    }
}