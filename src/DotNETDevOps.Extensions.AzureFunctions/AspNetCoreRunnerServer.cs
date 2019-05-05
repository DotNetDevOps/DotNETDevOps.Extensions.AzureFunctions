using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetDevelopmentRelativePathAttribute : Attribute
    {

        public AspNetDevelopmentRelativePathAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }

    public class AspNetCoreRunnerServer<TWrapper, T> : IAspNetCoreServer where T : class
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _host.Dispose();
            }
        }

        private TaskCompletionSource<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>> _applicationSource;







        public AspNetCoreRunnerServer(IServiceProvider serviceProvider, Microsoft.Azure.WebJobs.ExecutionContext executionContext)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AspNetCoreRunnerServer<TWrapper, T>>>();

            logger.LogInformation("Creating AspNetCoreRunnerServer");

            _applicationSource = new TaskCompletionSource<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>>();

            var builder = new WebHostBuilder();


            if (serviceProvider.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>().IsDevelopment())
            {
                logger.LogInformation("Running in development mode");
                var path = typeof(TWrapper).GetCustomAttribute<AspNetDevelopmentRelativePathAttribute>();
                if (path != null)
                {
                    var localPath = Path.Combine(Directory.GetCurrentDirectory(), path.Path);
                    logger.LogInformation("setting content root: {path}", localPath);
                    builder.UseContentRoot(localPath);
                }
            }
            else
            {
                logger.LogInformation("setting content root: {path}", executionContext.FunctionAppDirectory);
                builder.UseContentRoot(executionContext.FunctionAppDirectory);
            }
            builder.ConfigureAppConfiguration((c, b) =>
            {
                var a = Directory.GetCurrentDirectory();

                //TODO Make extensibility to configure webuilder configuration.
            })
                .UseStartup<T>();



            var _host = builder.UseServer(this);

            builder.Build().StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    _applicationSource.SetException(task.Exception);
            });
        }

        private bool _disposed = false;
        private IWebHost _host;

        public Task<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>> GetApplicationAsync()
        {
            return _applicationSource.Task;


        }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            this._applicationSource.SetResult((IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>)application);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
