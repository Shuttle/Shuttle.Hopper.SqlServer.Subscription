namespace Shuttle.Hopper.SqlServer.Subscription;

public interface ISqlServerSubscriptionDbContextFactory
{
    SqlServerSubscriptionDbContext Create();
}