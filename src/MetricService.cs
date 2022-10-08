namespace AzQueueMetricExporter;

using System.Diagnostics.Metrics;

public class MetricService : BackgroundService2
{
    private readonly AzureQueueMetricReceiver receiver;
    private readonly MetricOptions options;

    public MetricService(
        AzureQueueMetricReceiver receiver,
        MetricOptions options)
    {
        this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var meter = new Meter(this.options.MeterName);
        var counter = meter.CreateCounter<double>(this.options.MetricName);

        while (!stoppingToken.IsCancellationRequested)
        {
            counter.Add(await this.receiver.GetMetricAsync(stoppingToken));
            await Task.Delay(this.options.Interval, stoppingToken);
        }
    }
}
