using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSqlServerSubscription(Action<SqlServerSubscriptionBuilder>? builder = null)
        {
            var sqlServerSubscriptionBuilder = new SqlServerSubscriptionBuilder(Guard.AgainstNull(services));

            builder?.Invoke(sqlServerSubscriptionBuilder);

            services.AddSingleton<IValidateOptions<SqlServerSubscriptionOptions>, SqlServerSubscriptionOptionsValidator>();

            services.AddOptions<SqlServerSubscriptionOptions>().Configure(options =>
            {
                options.ConnectionStringName = sqlServerSubscriptionBuilder.Options.ConnectionStringName;
                options.Schema = sqlServerSubscriptionBuilder.Options.Schema;
                options.ConfigureDatabase = sqlServerSubscriptionBuilder.Options.ConfigureDatabase;
            });

            services.AddSingleton<ISubscriptionQuery, SubscriptionQuery>();
            services.AddSingleton<SubscriptionObserver>();
            services.AddSingleton<IHostedService, SubscriptionHostedService>();

            services.AddDbContextFactory<SqlServerSubscriptionDbContext>((serviceProvider, dbContextFactoryBuilder) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                var connectionString = configuration.GetConnectionString(sqlServerSubscriptionBuilder.Options.ConnectionStringName);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException(string.Format(Resources.ConnectionStringException, sqlServerSubscriptionBuilder.Options.ConnectionStringName));
                }

                dbContextFactoryBuilder.UseSqlServer(connectionString);
            });


            return services;
        }
    }
}