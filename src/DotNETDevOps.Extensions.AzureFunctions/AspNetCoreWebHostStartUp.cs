using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(DotNETDevOps.Extensions.AzureFunctions.ExtensionStartup))]

namespace DotNETDevOps.Extensions.AzureFunctions
{

    public class AspNetCoreWebHostStartUp<T> : IWebJobsStartup
        where T : class,IWebHostBuilderExtension
      
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddTransient<T>();
        }
    }
    internal class ExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<AspNetCoreExtension>();
            builder.Services.Add(new ServiceDescriptor(typeof(IAspNetCoreRunner<>), typeof(AspNetCoreRunner<>), ServiceLifetime.Singleton));
        }
    }
}
