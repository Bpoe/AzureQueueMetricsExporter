namespace AzQueueMetricExporter;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using OpenTelemetry.Resources;

public static class Program
{
    private static readonly TagList Attributes = new()
    {
        { "messaging.system", "AzureStorageQueue" },
    };

    public static async Task Main(string[] args)
    {
        var configuration = CreateDefaultConfiguration(args)
            .Build();

        var options = configuration.Get<Options>() ?? new Options();

        var metric = new Meter(options.MeterName)
            .CreateHistogram<double>(
                options.MetricName,
                description: "Measures the approximate count of messages in the queue.");

        using var meterProvider = BuildMeterProvider(options);

        var queueClient = CreateQueueClient(configuration, options);

        while (true)
        {
            metric.Record((await queueClient.GetPropertiesAsync())
                .Value
                .ApproximateMessagesCount,
                Attributes);
            await Task.Delay(options.Interval);
        }
    }

    private static IConfigurationBuilder CreateDefaultConfiguration(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_Environment") ?? "Production";

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
            .SetResourceBuilder(ResourceBuilder.CreateDefault())
            .AddOtlpExporter()
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
        if (options?.QueueUri is not null)
        {
            var credential = new DefaultAzureCredential(configuration.Get<DefaultAzureCredentialOptions>());
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