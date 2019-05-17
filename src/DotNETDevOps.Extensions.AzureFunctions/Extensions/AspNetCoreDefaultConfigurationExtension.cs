using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions.Extensions
{
    public static class AspNetCoreDefaultConfigurationExtension
    {
        public static IWebHostBuilder UseAppSettingsJson(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                configurationBuilder
                    //  .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);

             
            });
        }
    }
}
