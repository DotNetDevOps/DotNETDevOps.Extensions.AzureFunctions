# DotNETDevOps.Extensions.AzureFunctions

Extensions for Azure Functions to run AspNetCore applications.
-----------------------------

## Usage

You may use the `AspNetDevelopmentRelativePathAttribute` to specify the relativepath from your function project to the project that has your aspnet core application. 
This allows it to use wwwroot folder from setting content root to your project folder. This is needed, since visual studio do not copy all files over when building/running in visual studio. 
On publish, everything works without needing this.

Remember to set `"ASPNETCORE_ENVIRONMENT": "Development"` in your ´local.settings.json`, otherwise the attribute wont be used.

Depend on `IAspNetCoreRunner<T>´ in your class that contains your function runner, and delegate the request to this for running the application. See example.


```
using IOBoard.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using System.Threading.Tasks;

[assembly: WebJobsStartup(typeof(AspNetCoreWebHostStartUp))]

namespace IOBoard.Portal.FunctionHost
{


    [AspNetDevelopmentRelativePath("../../../../../apps/IO-Board.Portal")]
    public class ServerlessApi
    {
        private readonly IAspNetCoreRunner<ServerlessApi> aspNetCoreRunner;

        public ServerlessApi(IAspNetCoreRunner<ServerlessApi> aspNetCoreRunner)
        {
            this.aspNetCoreRunner = aspNetCoreRunner;
        }


        [FunctionName("PortalBackend")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*all}")]HttpRequest req, ExecutionContext executionContext)
            => aspNetCoreRunner.RunAsync<PortalHostStartup>(req,executionContext);

       

    }
}
```


