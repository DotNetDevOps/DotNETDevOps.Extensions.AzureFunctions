using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Host.Config;
using System.Linq;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreFunctionServer : IAspNetCoreServer
    {
        private bool _disposed = false;
        private IWebHost _host;

        private TaskCompletionSource<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>> _applicationSource;

        public IFeatureCollection Features { get; } = new FeatureCollection();





        public AspNetCoreFunctionServer(Microsoft.Azure.WebJobs.ExecutionContext executionContext, AspNetCoreRunnerAttribute aspNetCoreRunnerAttribute,IServiceProvider serviceProvider)
        {
           var logger = serviceProvider.GetRequiredService<ILogger<AspNetCoreFunctionServer>>();

            logger.LogInformation($"Creating {nameof(AspNetCoreFunctionServer)}");

            _applicationSource = new TaskCompletionSource<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>>();

            var builder = new WebHostBuilder(); 

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(executionContext);
                services.AddSingleton<IStartupFilter, HttpContextAccessorStartupFilter>();

                var type = Type.GetType("Microsoft.Azure.WebJobs.OrchestrationClientAttribute, Microsoft.Azure.WebJobs.Extensions.DurableTask");
                if (type != null)
                {
                    var clientType = Type.GetType("Microsoft.Azure.WebJobs.DurableOrchestrationClient, Microsoft.Azure.WebJobs.Extensions.DurableTask");
                    var iClientType = Type.GetType("Microsoft.Azure.WebJobs.IDurableOrchestrationClient, Microsoft.Azure.WebJobs.Extensions.DurableTask");
                    var extensionType = Type.GetType("Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableTaskExtension, Microsoft.Azure.WebJobs.Extensions.DurableTask");


                    services.AddSingleton(type);

                    if (clientType!=null)
                    {
                        RegisterDurableClient(aspNetCoreRunnerAttribute, serviceProvider, services, type, clientType, extensionType);
                    }
                    if (iClientType != null)
                    {
                        RegisterDurableClient(aspNetCoreRunnerAttribute, serviceProvider, services, type, iClientType, extensionType);
                        
                    }




                }

            });

            builder.UseContentRoot(executionContext.FunctionAppDirectory);

            
            if (aspNetCoreRunnerAttribute.WebBuilderExtension != null)
            {
                var configure = serviceProvider.GetService(aspNetCoreRunnerAttribute.WebBuilderExtension) as IWebHostBuilderExtension;
                if (configure != null)
                {
                    configure.ConfigureWebHostBuilder(executionContext, builder);
                    // builder.ConfigureAppConfiguration(configure.ConfigureAppConfiguration);
                }

            }


            builder.UseStartup(aspNetCoreRunnerAttribute.Startup);



            var _host = builder.UseServer(this);

            builder.Build().StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    _applicationSource.SetException(task.Exception);
            });
        }

        private static void RegisterDurableClient(AspNetCoreRunnerAttribute aspNetCoreRunnerAttribute, IServiceProvider serviceProvider, IServiceCollection services, Type type, Type clientType, Type extensionType)
        {
            services.AddSingleton(clientType, sp =>
            {
                var configurations = serviceProvider.GetServices<IExtensionConfigProvider>();

                var durableTaskExtension = configurations.Where(t => t.GetType() == extensionType).FirstOrDefault();

                var getclient = durableTaskExtension.GetType().GetMethod("GetClient", BindingFlags.NonPublic | BindingFlags.Instance);
                var orchestrationClientAttribute = Activator.CreateInstance(type);
                type.GetProperty("TaskHub").SetValue(orchestrationClientAttribute, aspNetCoreRunnerAttribute.TaskHub);
                type.GetProperty("ConnectionName").SetValue(orchestrationClientAttribute, aspNetCoreRunnerAttribute.ConnectionName);


                var client = getclient.Invoke(durableTaskExtension, new object[] { orchestrationClientAttribute });

                return client;
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _host.Dispose();
            }
        }
    }
}
