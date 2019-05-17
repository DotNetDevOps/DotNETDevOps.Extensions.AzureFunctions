using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace DotNETDevOps.Extensions.AzureFunctions
{



    /// <summary>
    /// This is registered when using [assembly: WebJobsStartup(typeof(AspNetCoreWebHostStartUp))] such that you can use 
    /// it as a dependency to the function class that host your aspnetcore function.
    /// 
    /// <code>
    ///  public class ServerlessApiFunction
    ///    {
    ///
    ///
    ///        private readonly IAspNetCoreRunner<ServerlessApiFunction> aspNetCoreRunner;
    ///
    ///    public ServerlessApiFunction(IAspNetCoreRunner<ServerlessApiFunction> aspNetCoreRunner)
    ///    {
    ///        this.aspNetCoreRunner = aspNetCoreRunner;
    ///    }
    ///
    ///    [FunctionName("AspNetCoreHost")]
    ///    public Task<IActionResult> Run(
    ///        [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*all}")]HttpRequest req, ExecutionContext executionContext, ILogger log)
    ///    => aspNetCoreRunner.RunAsync<Startup>(req, executionContext);
    ///}
    /// </code>
    /// </summary>
    /// <typeparam name="TWrapper">The class that host your aspnet core function, used to resolve configuration attributes</typeparam>
    public class AspNetCoreRunner<TWrapper> : IAspNetCoreRunner<TWrapper>
    {
        
        private ConcurrentDictionary<Type, IAspNetCoreServer> hosts = new ConcurrentDictionary<Type, IAspNetCoreServer>(); 
        public Task<IActionResult> RunAsync<T>(HttpRequest req, Microsoft.Azure.WebJobs.ExecutionContext executionContext) where T : class
        {
            
            
            return Task.FromResult(
                new AspNetCoreRunnerActionResult(
                    hosts.GetOrAdd(typeof(T), (t) => new AspNetCoreRunnerServer<TWrapper,T>(req.HttpContext.RequestServices, executionContext))
                    ) as IActionResult);
        }


    }
}
