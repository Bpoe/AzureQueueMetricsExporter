namespace AzQueueMetricExporter;

public class QueueOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = string.Empty;

    public Uri? Uri { get; set; }
}
