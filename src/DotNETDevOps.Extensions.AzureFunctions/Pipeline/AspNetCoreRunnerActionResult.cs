using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
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

            var CreateContext = application.GetType().GetMethod("CreateContext");
            var appContext = CreateContext.Invoke(application,new object[] { context.HttpContext.Features }); 
            var method = application.GetType().GetMethod("ProcessRequestAsync"); 
            var task = method.Invoke(application, new[] { appContext });
          
            if(task is Task tasktask)
            {
                await tasktask;
            }
           
          //  await application.ProcessRequestAsync(context.HttpContext);

        }

    }
}
