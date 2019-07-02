using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Azure.WebJobs;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreRunner : IAspNetCoreRunner
    {
        private readonly IServiceProvider serviceProvider;
        private readonly AspNetCoreRunnerAttribute aspNetCoreRunnerAttribute;
        private readonly ValueBindingContext valueBindingContext;

        public AspNetCoreRunner(IServiceProvider serviceProvider, AspNetCoreRunnerAttribute aspNetCoreRunnerAttribute, ValueBindingContext valueBindingContext)
        {
            this.serviceProvider = serviceProvider;
            this.aspNetCoreRunnerAttribute = aspNetCoreRunnerAttribute;
            this.valueBindingContext = valueBindingContext;
        }

        private static ConcurrentDictionary<AspNetCoreRunnerAttribute, Lazy<IAspNetCoreServer>> hosts = new ConcurrentDictionary<AspNetCoreRunnerAttribute, Lazy<IAspNetCoreServer>>();

        

        public Task<IActionResult> RunAsync(ExecutionContext executionContext)  
        { 
            return Task.FromResult(
                new AspNetCoreRunnerActionResult(
                    hosts.GetOrAdd(aspNetCoreRunnerAttribute, (t)=>new Lazy<IAspNetCoreServer>( () => new AspNetCoreFunctionServer(executionContext, aspNetCoreRunnerAttribute,serviceProvider))).Value
                    ) as IActionResult);    
        }

       
    }
}
