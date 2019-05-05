using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DotNETDevOps.Extensions.AzureFunctions
{
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
