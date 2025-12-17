namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionOptions
{
    public const string SectionName = "Shuttle:SqlServer:Subscription";

    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public string ConnectionString { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public bool EnsureSchema { get; set; } = true;
}