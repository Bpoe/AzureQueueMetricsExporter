namespace AzQueueMetricExporter;

using Azure.Storage.Queues;

public class AzureQueueMetricReceiver
{
    private readonly QueueClient queueClient;

    public AzureQueueMetricReceiver(QueueClient queueClient)
        => this.queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient));

    public async Task<double> GetMetricAsync(CancellationToken cancellationToken = default)
        => (await queueClient.GetPropertiesAsync(cancellationToken))
            .Value
            .ApproximateMessagesCount;
}
