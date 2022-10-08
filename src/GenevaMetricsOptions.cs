namespace AzQueueMetricExporter;

public class GenevaMetricsOptions
{
    public string LogsConnectionString { get; set; } = "EtwSession=OpenTelemetry";

    public string MetricAccountName { get; set; } = string.Empty;

    public string MetricNamespace { get; set; } = string.Empty;

    public string Geography { get; set; } = string.Empty;

    public string GetMetricsConnectionString()
        => $"Account={this.MetricAccountName};Namespace={this.MetricNamespace}";
}
