using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionBuilder(IServiceCollection services)
{
    public SqlServerSubscriptionOptions Options
    {
        get;
        set => field = Guard.AgainstNull(value);
    } = new();

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);
}