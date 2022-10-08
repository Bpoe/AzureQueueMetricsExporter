using AzQueueMetricExporter;

try
{
    await Host
        .CreateDefaultBuilder(args)
        .ConfigureServices(Startup.ConfigureServices)
        .Build()
        .RunAsync();
}
catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
{
    // This is OK
}
