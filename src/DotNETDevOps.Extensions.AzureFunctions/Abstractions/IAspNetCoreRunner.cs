using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IAspNetCoreRunner
    {
        Task<IActionResult> RunAsync<T>(Microsoft.AspNetCore.Http.HttpRequest req, Microsoft.Azure.WebJobs.ExecutionContext executionContext) where T : class;
    }
}
