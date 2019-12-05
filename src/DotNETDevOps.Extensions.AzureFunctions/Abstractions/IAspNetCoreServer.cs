using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IApplication
    {
        Task ProcessRequestAsync(ActionContext context);
    }
    public interface IAspNetCoreServer : IServer { 

        Task<IApplication> GetApplicationAsync();
    }
}
