﻿using Microsoft.AspNetCore.Hosting.Server;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IAspNetCoreServer : IServer { 

        Task<object> GetApplicationAsync();
    }
}
