using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RLDatix.OpenTelemetry.API.Observability
{
    public static class OpenTelemetryServiceCollectionExtensions
    {

        //service name and service version are supposed to be set as env variables with keys: serviceName, serviceVersion
        private static readonly string ServiceName = Environment.GetEnvironmentVariable("serviceName") ?? "Labs.API";
        private static readonly string ServiceVersion = Environment.GetEnvironmentVariable("serviceVersion") ?? "version unknown";

        public static IServiceCollection AddPlatformOpenTelemetry(this IServiceCollection serviceCollection, IConfiguration configuration)
        {

            var resource = ResourceBuilder.CreateDefault().AddService(ServiceName, serviceVersion: ServiceVersion);

            //instance of OtlpExporterOptions
            var otlpOptions = new OtlpExporterOptions();
            configuration.Bind(nameof(OtlpExporterOptions), otlpOptions);

            //configuring the resources, traces and instrumenting with tags enrichment and additional standardized data
            serviceCollection.AddOpenTelemetry()
                .WithLogging(loggerProviderBuilder => loggerProviderBuilder
                    .SetResourceBuilder(resource)
                    .AddProcessor(new ActivityEventLogProcessor())
                    .AddOtlpExporter(exporterOption =>
                    {
                        exporterOption.Endpoint = otlpOptions.Endpoint;
                        exporterOption.Headers = otlpOptions.Headers;
                        exporterOption.TimeoutMilliseconds = otlpOptions.TimeoutMilliseconds;
                    })
                    .AddConsoleExporter())
                .WithMetrics(meterProviderBuilder => meterProviderBuilder
                    .SetResourceBuilder(resource)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(exporterOption =>
                    {
                        exporterOption.Endpoint = otlpOptions.Endpoint;
                        exporterOption.Headers = otlpOptions.Headers;
                        exporterOption.TimeoutMilliseconds = otlpOptions.TimeoutMilliseconds;
                    })
                    .AddConsoleExporter())
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .SetResourceBuilder(resource)
                        .AddAspNetCoreInstrumentation(instrumentationOptions =>
                        {
                            instrumentationOptions.RecordException = true;
                            instrumentationOptions.EnrichWithHttpRequest = (activity, httpRequest) =>
                            {
                                activity.SetTag("host.id", configuration.GetValue<string>("HOST_ID"));
                                activity.SetTag("host.name", Environment.MachineName);
                                activity.SetTag("host.type", Environment.OSVersion.Platform.ToString().ToLower());
                                activity.SetTag("host.version", Environment.OSVersion.VersionString);
                            };

                            instrumentationOptions.EnrichWithException = (activity, exception) =>
                            {
                                if (exception.Source != null)
                                {
                                    activity.SetTag("exception.source", exception.Source);
                                }
                            };
                        })
                        .AddOtlpExporter(exporterOption =>
                        {
                            exporterOption.Endpoint = otlpOptions.Endpoint;
                            exporterOption.Headers = otlpOptions.Headers;
                            exporterOption.TimeoutMilliseconds = otlpOptions.TimeoutMilliseconds;
                        })
                        .AddConsoleExporter();
                });

            return serviceCollection;
        }
    }
}
