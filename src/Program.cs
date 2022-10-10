namespace AzQueueMetricExporter;

using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection;
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
        var configuration = CreateDefaultConfiguration(args)
            .Build();

        var options = configuration.Get<Options>() ?? new Options();

        var metric = new Meter(options.MeterName)
            .CreateHistogram<double>(options.MetricName);

        using var meterProvider = BuildMeterProvider(options);

        var queueClient = CreateQueueClient(configuration, options);

        while (true)
        {
            metric.Record((await queueClient.GetPropertiesAsync())
                .Value
                .ApproximateMessagesCount);
            await Task.Delay(options.Interval);
        }
    }

    private static IConfigurationBuilder CreateDefaultConfiguration(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_Environment");

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

        if (string.Equals("Development", environmentName, StringComparison.CurrentCultureIgnoreCase))
        {
            try
            {
                var appAssembly = Assembly.GetExecutingAssembly();
                config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: true);
            }
            catch (FileNotFoundException)
            {
                // The assembly cannot be found, so just skip it.
            }
        }

        return config
            .AddEnvironmentVariables()
            .AddCommandLine(args);
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