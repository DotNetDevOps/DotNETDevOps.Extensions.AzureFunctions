using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNETDevOps.Extensions.AzureFunctions;
using DotNETDevOps.Extensions.AzureFunctions.ApplicationInsights;
using FunctionApp6;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

[assembly: WebJobsStartup(typeof(WebJobStartup))]

namespace FunctionApp6
{

    public static class ApplicationInsightsExtensions
    {
        public static void AddDependencyTelemetrySamplingProcessor(this IWebJobsBuilder builder, int percentage = 100)
        {
            var tctype = Type.GetType("Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration, Microsoft.ApplicationInsights");
            if (tctype != null)
            {
                var configDescriptor = builder.Services.SingleOrDefault(tc => tc.ServiceType == tctype);
                if (configDescriptor?.ImplementationFactory != null)
                {
                    var implFactory = configDescriptor.ImplementationFactory;
                    builder.Services.Remove(configDescriptor);
                    builder.Services.AddSingleton(provider =>
                    {
                        if (implFactory.Invoke(provider) is TelemetryConfiguration config)
                        {
                            config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastRequests(next));
                            config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastDependencies(next));
                            config.TelemetryProcessorChainBuilder.Build();
                            return config;
                        }
                        return null;
                    });
                }
            }
        }
    }
    public class WebJobStartup : IWebJobsStartup
    {
        //https://github.com/Azure/azure-functions-host/issues/3741
        public void Configure(IWebJobsBuilder builder)
        {
            //  builder.AddDependencyTelemetrySamplingProcessor();
          
            
            //builder.Services.AddSingleton<ITelemetryModule>(provider =>
            //{
            //    var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            //    var appIdProvider = provider.GetService<IApplicationIdProvider>();


            //    return new RequestTrackingTelemetryModule(appIdProvider);

            //});



            builder.Services.AddSingleton<IWebHostBuilderExtension<Startup>, WebBuilderExtension>();
           // builder.Services.AddTransient<ITelemetryProcessor, AggressivelySampleFastRequests>();
        }
    }

    public class WebBuilderExtension : IWebHostBuilderExtension<Startup>
    {
        

        public void ConfigureWebHostBuilder(ExecutionContext executionContext, WebHostBuilder builder, IServiceProvider serviceProvider)
        {

            builder.UseSerilog((context,configuration)=>
            {

                configuration.WriteTo
                    .ApplicationInsights(serviceProvider.GetService<TelemetryConfiguration>(), TelemetryConverter.Traces);
            });
            //var config = serviceProvider.GetService<TelemetryConfiguration>();
            //var modules = serviceProvider.GetService<IEnumerable<ITelemetryModule>>();
            //foreach (var module in modules)
            //{
            //    module.Initialize(config);
            //}

            builder.ConfigureServices(collection =>
            {

                //THIS IS DONE BY FRAMEWORK Automatically

                //var tc = serviceProvider.GetService<TelemetryConfiguration>(); if (tc != null)
                //{
                //    collection.AddSingleton(tc);
                //}


                


            });


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

    //public class Function1
    //{
    //    public Function1(TelemetryConfiguration telemetryConfiguration)
    //    {

    //    }
    //    [FunctionName("Function1")]
    //    public void Run([TimerTrigger("0 */5 * * * *", RunOnStartup =true  )]TimerInfo myTimer, ILogger log)
    //    {
    //        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
    //    }
    //}
    public class test : IApplicationIdProvider
    {
        public bool TryGetApplicationId(string instrumentationKey, out string applicationId)
        {
            applicationId = "adsad";
            return true;
        }
    }
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //    services.AddApplicationInsightsTelemetry();

             
        }

        public void Configure(IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostingEnvironment env)
        {
            app.AddFastAndDependencySampler();
            var config = app.ApplicationServices.GetService<TelemetryConfiguration>();
            app.UseSerilogRequestLogging(); // <-- Add this line
            //config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastRequests(next));
            //config.TelemetryProcessorChainBuilder.Use(next => new AggressivelySampleFastDependencies(next));
            //config.TelemetryProcessorChainBuilder.Build();

            app.Map("/api/test", builderinner => {
                builderinner.Use((ctx,next) => {
                    var a = ctx.RequestServices.GetService<TelemetryConfiguration>();
                    throw new Exception("A");
                
                });
            });
            app.Run(async ctx =>
            {
                ctx.RequestServices.GetService<ILogger<Startup>>().LogWarning("TEST");
                await ctx.Response.WriteAsync("Hello world");
                
            });
        
        }
        
    }
    public class ServerlessApi
    {


        [FunctionName("AspNetCoreHost")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*all}")]HttpRequest req,
            [AspNetCoreRunner(Startup = typeof(Startup))] IAspNetCoreRunner aspNetCoreRunner,
            ExecutionContext executionContext)
        {
            return aspNetCoreRunner.RunAsync(executionContext);
        }


    }
}
