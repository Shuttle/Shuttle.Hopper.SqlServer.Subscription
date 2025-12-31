using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.SqlServer.Subscription;

public static class HopperBuilderExtensions
{
    extension(HopperBuilder hopperBuilder)
    {
        public HopperBuilder UseSqlServerSubscription(Action<SqlServerSubscriptionBuilder>? builder = null)
        {
            var services = hopperBuilder.Services;
            var sqlServerSubscriptionBuilder = new SqlServerSubscriptionBuilder(services);

            builder?.Invoke(sqlServerSubscriptionBuilder);

            services.AddSingleton<IValidateOptions<SqlServerSubscriptionOptions>, SqlServerSubscriptionOptionsValidator>();

            services.AddOptions<SqlServerSubscriptionOptions>().Configure(options =>
            {
                options.ConnectionString = sqlServerSubscriptionBuilder.Options.ConnectionString;
                options.Schema = sqlServerSubscriptionBuilder.Options.Schema;
                options.ConfigureDatabase = sqlServerSubscriptionBuilder.Options.ConfigureDatabase;
            });

            services.AddSingleton<ISubscriptionQuery, SubscriptionQuery>();
            services.AddSingleton<SubscriptionObserver>();
            services.AddHostedService<SubscriptionHostedService>();

            services.AddDbContextFactory<SqlServerSubscriptionDbContext>((_, dbContextFactoryBuilder) =>
            {
                dbContextFactoryBuilder.UseSqlServer(sqlServerSubscriptionBuilder.Options.ConnectionString);
            });

            return hopperBuilder;
        }
    }
}