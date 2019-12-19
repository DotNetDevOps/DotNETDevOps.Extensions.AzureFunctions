using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Builder;
using AspNetCore30SimplePlus;
using DotNETDevOps.Extensions.AzureFunctions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore30SimplePlus
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddHealthChecks();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/.well-known/ready", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("ready"),
                });

                endpoints.MapHealthChecks("/.well-known/live", new HealthCheckOptions
                {
                    Predicate = (_) => false
                });

                endpoints.MapControllers();


            });

            app.Run(r => r.Response.WriteAsync("HELLO WORLD"));
        }
    }
   
    public class ServerlessAspNetCore
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
