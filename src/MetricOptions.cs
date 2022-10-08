namespace AzQueueMetricExporter;

public class MetricOptions
{
    public string MeterName { get; set; } = "AzureStorage";

    public string MetricName { get; set; } = "Queue_ApproximateMessages_Count";

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
}

