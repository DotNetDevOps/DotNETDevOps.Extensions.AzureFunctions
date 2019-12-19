using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

 
namespace DotNETDevOps.Extensions.AzureFunctions
{

    public class AspNetCoreWebHostStartUp<TWebHostBuilder,TStartup> : IWebJobsStartup
        where TWebHostBuilder : class,IWebHostBuilderExtension<TStartup>
      
    {
        public virtual   void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddTransient<IWebHostBuilderExtension<TStartup>,TWebHostBuilder>();
        }
    }
}
