namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionOptions
{
    public const string SectionName = "Shuttle:SqlServer:Subscription";

    public string ConnectionString { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public bool ConfigureDatabase { get; set; } = true;
}