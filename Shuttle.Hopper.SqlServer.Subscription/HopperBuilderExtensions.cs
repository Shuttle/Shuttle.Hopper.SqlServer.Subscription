using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.SqlServer.Subscription;

public static class HopperBuilderExtensions
{
    extension(HopperBuilder hopperBuilder)
    {
        public HopperBuilder UseSqlServerSubscription(Action<SqlServerSubscriptionOptions>? configureOptions)
        {
            var services = hopperBuilder.Services;

            services.AddOptions<SqlServerSubscriptionOptions>().Configure(options =>
            {
                configureOptions?.Invoke(options);
            });

            services.AddSingleton<IValidateOptions<SqlServerSubscriptionOptions>, SqlServerSubscriptionOptionsValidator>();

            services.AddSingleton<ISubscriptionQuery, SubscriptionQuery>();
            services.AddSingleton<SubscriptionObserver>();
            services.AddHostedService<SubscriptionHostedService>();

            services.AddDbContextFactory<SqlServerSubscriptionDbContext>((serviceProvider, dbContextFactoryBuilder) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SqlServerSubscriptionOptions>>();

                dbContextFactoryBuilder.UseSqlServer(options.Value.ConnectionString);
            });

            return hopperBuilder;
        }
    }
}