namespace AzQueueMetricExporter;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        ConfigureServices(context.Configuration, services);
    }

    public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        var metricOptions = configuration.GetSection(nameof(MetricOptions)).Get<MetricOptions>() ?? new MetricOptions();
        var genevaOptions = configuration.GetSection(nameof(GenevaMetricsOptions)).Get<GenevaMetricsOptions>();
        var queueOptions = configuration.GetSection(nameof(QueueOptions)).Get<QueueOptions>();
        var exporterOptions = configuration.GetSection(nameof(ExporterOptions)).Get<ExporterOptions>();
        var defaultAzureCredentialOptions = configuration.GetSection(nameof(DefaultAzureCredentialOptions)).Get<DefaultAzureCredentialOptions>();

        services.AddOpenTelemetryMetrics(builder =>
        {
            builder
                .AddMeter(metricOptions.MeterName)
                .AddOtlpExporter();

            if (exporterOptions is not null && exporterOptions.EnableConsole)
            {
                builder.AddConsoleExporter();
            }

            if (genevaOptions is not null)
            {
                builder.AddGenevaMetricExporter(options =>
                {
                    options.ConnectionString = genevaOptions.GetMetricsConnectionString();
                });
            }
        });

        if (defaultAzureCredentialOptions is not null)
        {
            services.AddSingleton(defaultAzureCredentialOptions);
        }

        services
            .AddSingleton<TokenCredential, DefaultAzureCredential>(s =>
            {
                var options = s.GetService<DefaultAzureCredentialOptions>();

                return options is null
                    ? new DefaultAzureCredential()
                    : new DefaultAzureCredential(options);
            })
            .AddSingleton(s =>
            {
                var credential = s.GetRequiredService<TokenCredential>();

                return CreateQueueClient(queueOptions, credential);
            })
            .AddSingleton(metricOptions)
            .AddSingleton<AzureQueueMetricReceiver>()
            .AddHostedService<MetricService>();
    }

    private static QueueClient CreateQueueClient(QueueOptions options, TokenCredential credential)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Uri != null)
        {
            return new QueueClient(options.Uri, credential);
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString)
            && !string.IsNullOrWhiteSpace(options.QueueName))
        {
            return new QueueClient(options.ConnectionString, options.QueueName);
        }

        throw new ArgumentException("QueueOptions are not valid.", nameof(options));
    }
}
