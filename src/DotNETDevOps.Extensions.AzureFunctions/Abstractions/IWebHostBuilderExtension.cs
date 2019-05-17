using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IWebHostBuilderExtension
    {

        void ConfigureWebHostBuilder(WebHostBuilder builder);
    }
}
