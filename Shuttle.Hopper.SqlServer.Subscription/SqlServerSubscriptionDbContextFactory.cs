using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionDbContextFactory(IOptions<SqlServerSubscriptionOptions> sqlServerSubscriptionOptions) : ISqlServerSubscriptionDbContextFactory
{
    private readonly SqlServerSubscriptionOptions _sqlServerSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlServerSubscriptionOptions).Value);

    public SqlServerSubscriptionDbContext Create()
    {
        var options = new DbContextOptionsBuilder<SqlServerSubscriptionDbContext>()
            .UseSqlServer(_sqlServerSubscriptionOptions.ConnectionString)
            .Options;

        return new(options, _sqlServerSubscriptionOptions.Schema);
    }
}
