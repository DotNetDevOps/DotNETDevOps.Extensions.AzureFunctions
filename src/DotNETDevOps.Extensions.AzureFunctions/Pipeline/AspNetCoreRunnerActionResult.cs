using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreRunnerActionResult : IActionResult
    {
        private readonly IAspNetCoreServer server;

        public AspNetCoreRunnerActionResult(IAspNetCoreServer server)
        {
            this.server = server ?? throw new System.ArgumentNullException(nameof(server));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Features.Set<IServiceProvidersFeature>(null);
            

            var application = await this.server.GetApplicationAsync();
           
            await application.ProcessRequestAsync(new Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context() { HttpContext = context.HttpContext });

        }

    }
}
