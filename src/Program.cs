namespace AzQueueMetricExporter;

using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = BuildConfiguration(args);

        var options = configuration.Get<Options>() ?? new Options();

        var counter = new Meter(options.MeterName)
            .CreateCounter<double>(options.MetricName);

        using var meterProvider = BuildMeterProvider(options);

        var queueClient = CreateQueueClient(configuration, options);

        while (true)
        {
            counter.Add((await queueClient.GetPropertiesAsync())
                .Value
                .ApproximateMessagesCount);
            await Task.Delay(options.Interval);
        }
    }

    private static IConfiguration BuildConfiguration(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_Environment");

        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    private static MeterProvider BuildMeterProvider(Options options)
    {
        var meterBuilder = Sdk
            .CreateMeterProviderBuilder()
            .AddMeter(options.MeterName);

        if (options is not null && options.EnableConsole)
        {
            meterBuilder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(options?.GenevaMetricAccountName)
            && !string.IsNullOrEmpty(options?.GenevaMetricNamespace))
        {
            meterBuilder.AddGenevaMetricExporter(exporterOptions =>
            {
                exporterOptions.ConnectionString
                    = $"Account={options.GenevaMetricAccountName};Namespace={options.GenevaMetricNamespace}";
            });
        }

        return meterBuilder.Build();
    }

    private static QueueClient CreateQueueClient(IConfiguration configuration, Options options)
    {
        var credential = new DefaultAzureCredential(configuration.Get<DefaultAzureCredentialOptions>());

        if (options?.QueueUri != null)
        {
            return new QueueClient(options.QueueUri, credential);
        }

        if (!string.IsNullOrWhiteSpace(options?.QueueConnectionString)
            && !string.IsNullOrWhiteSpace(options?.QueueName))
        {
            return new QueueClient(options.QueueConnectionString, options.QueueName);
        }

        throw new ArgumentException("QueueOptions are not valid.", nameof(options));
    }
}