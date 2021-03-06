﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public interface IWebHostBuilderExtension<TStartup> : IBuilderExtension
    {

      
    }
    public interface IBuilderExtension
    {
        void ConfigureWebHostBuilder(Microsoft.Azure.WebJobs.ExecutionContext executionContext, IWebHostBuilder builder, System.IServiceProvider serviceProvider);
    }
}
