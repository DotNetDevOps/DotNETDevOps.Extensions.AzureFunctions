using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IAspNetCoreRunner 
    {
        Task<IActionResult> RunAsync(Microsoft.Azure.WebJobs.ExecutionContext executionContext);

    }
    public interface IAspNetCoreRunner<TWrapper>
    {
        Task<IActionResult> RunAsync<T>(Microsoft.AspNetCore.Http.HttpRequest req, Microsoft.Azure.WebJobs.ExecutionContext executionContext) where T : class;
    }
}
