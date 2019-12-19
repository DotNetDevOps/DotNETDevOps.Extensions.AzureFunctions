using DotNETDevOps.Extensions.AzureFunctions.HealthCheck;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(DotNETDevOps.Extensions.AzureFunctions.ExtensionStartup))]

namespace DotNETDevOps.Extensions.AzureFunctions
{
    internal class ExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<AspNetCoreExtension>();
            builder.Services.AddSingleton<HealthCheckManager>();
            builder.Services.AddHttpClient();
        }
    }
}
