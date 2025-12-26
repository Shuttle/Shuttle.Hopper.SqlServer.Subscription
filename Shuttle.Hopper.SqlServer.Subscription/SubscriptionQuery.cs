using System.Data;
using Microsoft.EntityFrameworkCore;
using Shuttle.Core.Contract;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriptionQuery(IDbContextFactory<SqlServerSubscriptionDbContext> dbContextFactory, IOptions<SqlServerSubscriptionOptions> sqlSubscriptionOptions) : ISubscriptionQuery
{
    private readonly IDbContextFactory<SqlServerSubscriptionDbContext> _dbContextFactory = Guard.AgainstNull(dbContextFactory);
    private readonly SqlServerSubscriptionOptions _sqlServerSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);

    public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(messageType);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = $@"
SELECT 
    InboxWorkQueueUri 
FROM 
    [{_sqlServerSubscriptionOptions.Schema}].[SubscriberMessageType] 
WHERE 
    MessageType = @MessageType
";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@MessageType";
        parameter.Value = messageType;
        command.Parameters.Add(parameter);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var result = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}