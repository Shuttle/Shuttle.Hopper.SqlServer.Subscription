using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.SqlServer.Subscription.Tests;

[TestFixture]
public class SqlServerSubscriptionServiceFixture
{
    private readonly Uri _workTransportUri = new("queue://./work");

    [Test]
    public async Task Should_be_able_perform_standard_subscriptions_async()
    {
        var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscriptionMode.Standard, [typeof(MessageTypeOne).FullName!]);

        var uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();

        Assert.That(uris.Count, Is.EqualTo(1));
        Assert.That(uris.ElementAt(0), Is.EqualTo(_workTransportUri));
    }

    [Test]
    public async Task Should_be_able_to_ignore_subscribe_async()
    {
        var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscriptionMode.Disabled, [typeof(MessageTypeOne).FullName!]);

        List<string> uris = [];

        try
        {
            uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();
        }
        catch
        {
            // ignore
        }

        Assert.That(uris.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_be_able_to_ensure_subscribe()
    {
        Assert.ThrowsAsync<ApplicationException>(async () => await ExerciseSubscriptionServiceAsync(SubscriptionMode.FailWhenMissing, [typeof(MessageTypeOne).FullName!]));
    }

    private async Task<ISubscriptionService> ExerciseSubscriptionServiceAsync(SubscriptionMode subscriptionMode, List<string> messageTypes)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<SqlServerSubscriptionServiceFixture>()
            .Build();

        var services = new ServiceCollection()
            .AddHopper(options =>
            {
                options.Inbox.WorkTransportUri = _workTransportUri;
                options.Subscription.Mode = subscriptionMode;
                options.Subscription.MessageTypes = messageTypes;
            })
            .UseSqlServerSubscription(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("Hopper") ?? throw new ApplicationException("A 'ConnectionString' with name 'Hopper' is required which points to a Sql Server database where the subscription table will be stored.");
                options.Schema = "subscription_fixture";
            })
            .Services;
        
        var serviceProvider = services.BuildServiceProvider();

        _ = serviceProvider.GetServices<IHostedService>().OfType<SubscriptionHostedService>().First();

        try
        {
            await using var dbContext = await serviceProvider.GetRequiredService<IDbContextFactory<SqlServerSubscriptionDbContext>>().CreateDbContextAsync();

            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM subscription_fixture.SubscriberMessageType");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        var subscriptionService = serviceProvider.GetRequiredService<ISubscriptionService>();

        var pipelineOptions = Options.Create(new PipelineOptions());

        await serviceProvider.GetRequiredService<SubscriptionObserver>().ExecuteAsync(new PipelineContext<Started>(new Pipeline(pipelineOptions, new Mock<IServiceProvider>().Object)));

        await serviceProvider.GetServices<IHostedService>().OfType<SubscriptionHostedService>().First().StopAsync(CancellationToken.None);

        return subscriptionService;
    }
}