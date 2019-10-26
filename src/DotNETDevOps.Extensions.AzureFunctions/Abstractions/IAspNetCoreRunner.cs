
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IAspNetCoreRunner 
    {
        Task<IActionResult> RunAsync(Microsoft.Azure.WebJobs.ExecutionContext executionContext);
    }
}
