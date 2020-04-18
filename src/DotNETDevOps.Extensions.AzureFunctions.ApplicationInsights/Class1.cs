using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
//using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions.ApplicationInsights
{
    //internal class StartupFilter : IStartupFilter
    //{
    //    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    //    {

    //        return app =>
    //        {
    //            app.AddFastAndDependencySampler();
    //            app.UseSerilogRequestLogging();
    //            next(app);
    //        };
    //    }
    //}
   
    public class FixNameProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;

        public FixNameProcessor(ITelemetryProcessor next)
        {
            // Next TelemetryProcessor in the chain
            _next = next;


        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request)
            {
                
                request.Name = $"{request.Properties["HttpMethod"]} {(request.Properties.TryGetValue("HttpPathBase", out var pathbase)?pathbase:"")}{request.Properties["HttpPath"]}";
                request.Context.Operation.Name = request.Name;
                request.Success = int.TryParse( request.ResponseCode,out var statuscode) && statuscode < 400;
                
            }

            // Send the item to the next TelemetryProcessor
            _next.Process(item);
        }
    }
    //public class TelemetryStartupFilter : IStartupFilter
    //{
         
    //    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    //    {

    //        return builder =>
    //        {


    //            next(builder);
    //            var a = builder.ApplicationServices.GetService<TeleetryConfigurationProvider>();
    //            var b = builder.ApplicationServices.GetService(a.serviceType);
    //            var d = b as TelemetryConfiguration;
    //            var config = builder.ApplicationServices.GetService<TelemetryConfiguration>();
    //          //  var c = builder.ApplicationServices.GetService(telemetryConfigurationType);
    //            if (config != null)
    //            {
    //                config.TelemetryProcessorChainBuilder.Use(n => new FixNameProcessor(n));
    //                config.TelemetryProcessorChainBuilder.Build();
    //            }
    //        };
    //    }
    //}
    public class SerilogTracesExtension<TStartup> : IWebHostBuilderExtension<TStartup>, IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
           
            builder.Services.AddSingleton<IWebHostBuilderExtension<TStartup>, SerilogTracesExtension<TStartup>>();
        }

        public void ConfigureWebHostBuilder(ExecutionContext executionContext, IWebHostBuilder builder, IServiceProvider serviceProvider)
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
            //   services.AddSingleton<IStartupFilter, StartupFilter>();
            });
         //   builder.ConfigureServices(s => s.AddSingleton<IStartupFilter, TelemetryStartupFilter>());
        }
    }

    //public static class ApplicationInsightsExtensions
    //{
    //    public static IApplicationBuilder AddFastAndDependencySampler(this IApplicationBuilder app)
    //    {
            
    //        var config = app.ApplicationServices.GetService<TelemetryConfiguration>();
    //        if (config != null)
    //        {
    //            config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastRequests(next));
    //            config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastDependencies(next));
    //            config.TelemetryProcessorChainBuilder.Build();
    //        }

    //        return app;
    //    }
    //}
    //public class AggressivelySampleFastDependencies : ITelemetryProcessor
    //{
    //    private ITelemetryProcessor _next;
    //    private readonly AdaptiveSamplingTelemetryProcessor samplingProcessor;
    //    public AggressivelySampleFastDependencies(ITelemetryProcessor next)
    //    {
    //        // Next TelemetryProcessor in the chain
    //        _next = next;
    //        this.samplingProcessor = new AdaptiveSamplingTelemetryProcessor(next)
    //        {
    //            ExcludedTypes = "Event", // exclude custom events from being sampled
    //            MaxTelemetryItemsPerSecond = 1, // default: 5 calls/sec
    //            SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(1), // default: 2 min
    //            SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(1), // default: 30 sec
    //            EvaluationInterval = TimeSpan.FromSeconds(1), // default: 15 sec
    //            InitialSamplingPercentage = 25, // default: 100%
    //        };

    //    }

    //    public void Process(ITelemetry item)
    //    {

    //        if (item is DependencyTelemetry dependency)
    //        {
    //            if (dependency.Duration.TotalMilliseconds < 100)
    //            {
    //                this.samplingProcessor.Process(item);
    //                return;
    //            }
    //        }

    //        // Send the item to the next TelemetryProcessor
    //        _next.Process(item);
    //    }
    //}
    public static class RequestExtensiosn
    {
        
        public static RequestTelemetry ToRequestTelemetry(this TraceTelemetry trace)
        {
            return new RequestTelemetry().FromTraceTelemetry(trace);
        }
        public static RequestTelemetry FromTraceTelemetry(this RequestTelemetry request, TraceTelemetry trace)
        {

            request.Context.ReadFrom(trace.Context);
            typeof(RequestTelemetry)
   .GetField("context", BindingFlags.Instance | BindingFlags.NonPublic)
   .SetValue(request, trace.Context);
          //  typeof().GetProperty("Context").SetValue(request, trace.Context);
            return request;
        }
        public static void ReadFrom(this TelemetryContext target, TelemetryContext source)
        {
            target.Operation.ReadFrom(source.Operation);
            target.Location.Ip = source.Location.Ip;
            target.Session.Id = source.Session.Id;
            target.Session.IsFirst = source.Session.IsFirst;
            target.User.ReadFrom(source.User);
        }
        public static void ReadFrom(this OperationContext target, OperationContext source)
        {
            target.Id = source.Id;
            target.ParentId = source.ParentId;
            target.Name = source.Name;
            target.SyntheticSource = source.SyntheticSource;
        }
        public static void ReadFrom(this UserContext target, UserContext source)
        {
            target.Id = source.Id;
            target.AccountId = source.AccountId;
            target.AuthenticatedUserId = source.AuthenticatedUserId;
            target.UserAgent = source.UserAgent;
            
        }
    }
    //public class AggressivelySampleFastRequests : ITelemetryProcessor
    //{
    //    private ITelemetryProcessor _next;
    //    private readonly AdaptiveSamplingTelemetryProcessor samplingProcessor;
    //    public AggressivelySampleFastRequests(ITelemetryProcessor next)
    //    {
    //        // Next TelemetryProcessor in the chain
    //        _next = next;
    //        this.samplingProcessor = new AdaptiveSamplingTelemetryProcessor(next)
    //        {
    //            ExcludedTypes = "Event", // exclude custom events from being sampled
    //            MaxTelemetryItemsPerSecond = 1, // default: 5 calls/sec
    //            SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(1), // default: 2 min
    //            SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(1), // default: 30 sec
    //            EvaluationInterval = TimeSpan.FromSeconds(1), // default: 15 sec
    //            InitialSamplingPercentage = 25, // default: 100%
    //        };

    //    }

    //    public void Process(ITelemetry item)
    //    {
    //        if (item is TraceTelemetry trace && trace.Properties.ContainsKey("SourceContext") && trace.Properties["SourceContext"] == RequestLoggingMiddleware)
    //        {
    //          //  var requestMap = trace.ToRequestTelemetry();
    //          //  requestMap.Duration = TimeSpan.FromMilliseconds(double.Parse(trace.Properties["Elapsed"]));
    //          //  requestMap.ResponseCode = trace.Properties["StatusCode"];
    //          //  requestMap.Success = int.Parse(requestMap.ResponseCode) < 400;
    //          //  requestMap.Name = trace.Properties["RequestMethod"] + " " + trace.Properties["RequestPath"];
    //          //  item = requestMap;
    //          //  item.Timestamp = trace.Timestamp;
                
    //          ////  requestMap.ItemTypeFlag = SamplingTelemetryItemTypes.Request;
    //          //  _next.Process(item);
    //          //  return;


    //            //HMM could i change it to a request?
    //            //https://github.com/microsoft/ApplicationInsights-aspnetcore/blob/a135f1f7d9da7beb11f9bcf20a30f7e779b739f2/NETCORE/src/Microsoft.ApplicationInsights.AspNetCore/DiagnosticListeners/Implementation/HostingDiagnosticListener.cs#L727

    //            //https://github.com/serilog/serilog-aspnetcore/issues/52
    //            //https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/AspNetCore/RequestLoggingMiddleware.cs
    //            // Serilog middleware is way simple to aspnet core AI, so whats the catch.
    //            // item = new RequestTelemetry()

    //            var duration = double.Parse(trace.Properties["Elapsed"]);
    //            if (duration < 500)
    //            {
    //                this.samplingProcessor.Process(item);
    //                return;
    //            }
    //        }

    //        if (item is RequestTelemetry request)
    //        {
    //            if (request.Duration < TimeSpan.FromMilliseconds(500) || request.ResponseCode == "200")
    //            {
    //                // let sampling processor decide what to do
    //                // with this fast incoming request
    //                this.samplingProcessor.Process(item);
    //                return;
    //            }


    //        }
            

    //        // Send the item to the next TelemetryProcessor
    //        _next.Process(item);
    //    }
    //    private const string RequestLoggingMiddleware = "Serilog.AspNetCore.RequestLoggingMiddleware";
    //}
}
