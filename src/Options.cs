namespace AzQueueMetricExporter;

using System;

public class Options
{
    public bool EnableConsole { get; set; } = false;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    public string GenevaMetricAccountName { get; set; } = string.Empty;

    public string GenevaMetricNamespace { get; set; } = string.Empty;

    public string MeterName { get; set; } = "AzureStorage";

    public string MetricName { get; set; } = "messaging.queue.message_count";

    public string QueueConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = string.Empty;

    public Uri? QueueUri { get; set; }
}
