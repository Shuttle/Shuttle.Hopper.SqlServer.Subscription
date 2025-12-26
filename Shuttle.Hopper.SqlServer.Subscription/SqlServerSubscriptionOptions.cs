namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionOptions
{
    public const string SectionName = "Shuttle:SqlServer:Subscription";

    public string ConnectionStringName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public bool ConfigureDatabase { get; set; } = true;
}