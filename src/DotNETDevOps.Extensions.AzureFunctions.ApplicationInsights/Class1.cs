using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;

namespace DotNETDevOps.Extensions.AzureFunctions.ApplicationInsights
{
    internal class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {

            return app =>
            {
                app.AddFastAndDependencySampler();
                app.UseSerilogRequestLogging();
                next(app);
            };
        }
    }
    public class SerilogTracesExtension<TStartup> : IWebHostBuilderExtension<TStartup>, IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
           
            builder.Services.AddSingleton<IWebHostBuilderExtension<TStartup>, SerilogTracesExtension<TStartup>>();
        }

        public void ConfigureWebHostBuilder(ExecutionContext executionContext, WebHostBuilder builder, IServiceProvider serviceProvider)
        {

            builder.UseSerilog((context, configuration) =>
            {
                configuration
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);

                var telemetryConfig = serviceProvider.GetService<TelemetryConfiguration>();

                if (telemetryConfig != null)
                {
                    configuration.WriteTo
                        .ApplicationInsights(serviceProvider.GetService<TelemetryConfiguration>(), TelemetryConverter.Traces);
                }

                if (!context.HostingEnvironment.IsProduction())
                {
                    configuration.WriteTo.Console();
                }

            });

            builder.ConfigureServices(services =>
            {
               services.AddSingleton<IStartupFilter, StartupFilter>();
            });

        }
    }

    public static class ApplicationInsightsExtensions
    {
        public static IApplicationBuilder AddFastAndDependencySampler(this IApplicationBuilder app)
        {
            
            var config = app.ApplicationServices.GetService<TelemetryConfiguration>();
            if (config != null)
            {
                config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastRequests(next));
                config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastDependencies(next));
                config.TelemetryProcessorChainBuilder.Build();
            }

            return app;
        }
    }
    public class AggressivelySampleFastDependencies : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;
        private readonly AdaptiveSamplingTelemetryProcessor samplingProcessor;
        public AggressivelySampleFastDependencies(ITelemetryProcessor next)
        {
            // Next TelemetryProcessor in the chain
            _next = next;
            this.samplingProcessor = new AdaptiveSamplingTelemetryProcessor(next)
            {
                ExcludedTypes = "Event", // exclude custom events from being sampled
                MaxTelemetryItemsPerSecond = 1, // default: 5 calls/sec
                SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(1), // default: 2 min
                SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(1), // default: 30 sec
                EvaluationInterval = TimeSpan.FromSeconds(1), // default: 15 sec
                InitialSamplingPercentage = 25, // default: 100%
            };

        }

        public void Process(ITelemetry item)
        {

            if (item is DependencyTelemetry dependency)
            {
                if (dependency.Duration.TotalMilliseconds < 100)
                {
                    this.samplingProcessor.Process(item);
                    return;
                }
            }

            // Send the item to the next TelemetryProcessor
            _next.Process(item);
        }
    }
    public class AggressivelySampleFastRequests : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;
        private readonly AdaptiveSamplingTelemetryProcessor samplingProcessor;
        public AggressivelySampleFastRequests(ITelemetryProcessor next)
        {
            // Next TelemetryProcessor in the chain
            _next = next;
            this.samplingProcessor = new AdaptiveSamplingTelemetryProcessor(next)
            {
                ExcludedTypes = "Event", // exclude custom events from being sampled
                MaxTelemetryItemsPerSecond = 1, // default: 5 calls/sec
                SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(1), // default: 2 min
                SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(1), // default: 30 sec
                EvaluationInterval = TimeSpan.FromSeconds(1), // default: 15 sec
                InitialSamplingPercentage = 25, // default: 100%
            };

        }

        public void Process(ITelemetry item)
        {
            if (item is TraceTelemetry trace && trace.Properties.ContainsKey("SourceContext") && trace.Properties["SourceContext"] == RequestLoggingMiddleware)
            {
                //HMM could i change it to a request?
                //https://github.com/microsoft/ApplicationInsights-aspnetcore/blob/a135f1f7d9da7beb11f9bcf20a30f7e779b739f2/NETCORE/src/Microsoft.ApplicationInsights.AspNetCore/DiagnosticListeners/Implementation/HostingDiagnosticListener.cs#L727

                //https://github.com/serilog/serilog-aspnetcore/issues/52
                //https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/AspNetCore/RequestLoggingMiddleware.cs
                // Serilog middleware is way simple to aspnet core AI, so whats the catch.
                // item = new RequestTelemetry()

                var duration = double.Parse(trace.Properties["Elapsed"]);
                if (duration < 500)
                {
                    this.samplingProcessor.Process(item);
                    return;
                }
            }

            if (item is RequestTelemetry request)
            {
                if (request.Duration < TimeSpan.FromMilliseconds(500) || request.ResponseCode == "200")
                {
                    // let sampling processor decide what to do
                    // with this fast incoming request
                    this.samplingProcessor.Process(item);
                    return;
                }


            }
            

            // Send the item to the next TelemetryProcessor
            _next.Process(item);
        }
        private const string RequestLoggingMiddleware = "Serilog.AspNetCore.RequestLoggingMiddleware";
    }
}
