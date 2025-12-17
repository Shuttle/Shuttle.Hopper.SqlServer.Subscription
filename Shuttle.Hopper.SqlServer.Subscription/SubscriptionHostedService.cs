using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SubscriptionHostedService : IHostedService
{
    private readonly PipelineOptions _pipelineOptions;

    public SubscriptionHostedService(IOptions<PipelineOptions> pipelineOptions)
    {
        _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);

        _pipelineOptions.PipelineCreated += PipelineCreated;
    }

    private Task PipelineCreated(PipelineEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (eventArgs.Pipeline.GetType() == typeof(StartupPipeline))
        {
            eventArgs.Pipeline.AddObserver<SubscriptionObserver>();
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated -= PipelineCreated;

        return Task.CompletedTask;
    }
}