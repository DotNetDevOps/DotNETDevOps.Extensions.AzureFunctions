using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreWebHostStartUp : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.Add(new ServiceDescriptor(typeof(IAspNetCoreRunner<>), typeof(AspNetCoreRunner<>), ServiceLifetime.Singleton));
        }
    }
}
