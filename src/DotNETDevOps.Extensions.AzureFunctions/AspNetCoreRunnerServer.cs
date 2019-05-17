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

    public class AspNetCoreRunnerServer<TWrapper, T> : IAspNetCoreServer where T : class
    {
        private bool _disposed = false;
        private IWebHost _host;

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


           

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(executionContext);
                services.AddSingleton<IStartupFilter, HttpContextAccessorStartupFilter>();

                var type= Type.GetType("Microsoft.Azure.WebJobs.OrchestrationClientAttribute, Microsoft.Azure.WebJobs.Extensions.DurableTask");
                if (type != null) {
                    var clientType = Type.GetType("Microsoft.Azure.WebJobs.DurableOrchestrationClient, Microsoft.Azure.WebJobs.Extensions.DurableTask");

                    services.AddSingleton(type);
                    services.AddSingleton(clientType);
                    //services.add

                    //var test = req.HttpContext.RequestServices.GetServices<IExtensionConfigProvider>();
                    //var dur = test.OfType<DurableTaskExtension>().FirstOrDefault();
                    //var getclient = dur.GetType().GetMethod("GetClient", BindingFlags.NonPublic | BindingFlags.Instance);

                    //var client = getclient.Invoke(dur, new object[] { new OrchestrationClientAttribute() });
                }

            });

            builder.UseContentRoot(executionContext.FunctionAppDirectory);

            var webhostconfiguration = typeof(TWrapper).GetCustomAttribute<WebHostBuilderAttribute>();
            if (webhostconfiguration != null)
            {
                var configure = serviceProvider.GetService(webhostconfiguration.Type) as IWebHostBuilderExtension;
                if(configure != null)
                {
                    configure.ConfigureWebHostBuilder(executionContext,builder);
                   // builder.ConfigureAppConfiguration(configure.ConfigureAppConfiguration);
                }
                
            }


            builder.UseStartup<T>();



            var _host = builder.UseServer(this);

            builder.Build().StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    _applicationSource.SetException(task.Exception);
            });
        }

       

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
