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
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class ApplicationWrapper<TContext> : IApplication
    {
        private IHttpApplication<TContext> application;
        private readonly MethodInfo CreateContextMethod;
        private readonly MethodInfo ProcessRequestAsyncMethod;

        public ApplicationWrapper(IHttpApplication<TContext> application)
        {
            this.application = application;
            CreateContextMethod = application.GetType().GetMethod("CreateContext");
            ProcessRequestAsyncMethod = application.GetType().GetMethod("ProcessRequestAsync");
        }

        public Task ProcessRequestAsync(ActionContext context)
        {
            context.HttpContext.Features.Set<IServiceProvidersFeature>(null);
            var parameters = new object[1] { context.HttpContext.Features };
            parameters[0] = CreateContextMethod.Invoke(application, parameters);

            var task = ProcessRequestAsyncMethod.Invoke(application, parameters);

            if (task is Task tasktask)
            {
                return tasktask;
            }
            throw new NotImplementedException();
        }
    }
    public class AspNetCoreFunctionServer : IAspNetCoreServer
    {
        private bool _disposed = false;
        private IWebHost _host;

        private TaskCompletionSource<IApplication> _applicationSource;

        public IFeatureCollection Features { get; } = new FeatureCollection();





        public AspNetCoreFunctionServer(Microsoft.Azure.WebJobs.ExecutionContext executionContext, AspNetCoreRunnerAttribute aspNetCoreRunnerAttribute, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AspNetCoreFunctionServer>>();

            logger.LogInformation($"Creating {nameof(AspNetCoreFunctionServer)}");

            _applicationSource = new TaskCompletionSource<IApplication>();

            // var webhostBuilder = new GenericWebHostBuilder(builder);
            var genericbuilder = Host.CreateDefaultBuilder().ConfigureWebHost(builder =>
            {  // new WebHostBuilder(); 

                

                builder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    var tctype = Type.GetType("Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration, Microsoft.ApplicationInsights");
                    if (tctype != null)
                    {
                        var tc = serviceProvider.GetService(tctype);
                        if (tc != null)
                            services.AddSingleton(tctype, tc);
                    }
                    services.AddSingleton(executionContext);
                    services.AddSingleton<IStartupFilter, HttpContextAccessorStartupFilter>();
                    var t = serviceProvider.GetService<TeleetryConfigurationProvider>();
                    services.AddSingleton(t);
                    if(t.serviceType != null)
                    {
                        var tc = serviceProvider.GetService(t.serviceType);
                        if(tc!=null)
                            services.AddSingleton(t.serviceType, tc);
                    }

                });

                builder.UseContentRoot(executionContext.FunctionAppDirectory);
                builder.ConfigureAppConfiguration((c, cbuilder) =>
                {
                    cbuilder
                    .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["ExecutionContext:FunctionName"] = executionContext.FunctionName,
                            ["ExecutionContext:FunctionAppDirectory"] = executionContext.FunctionAppDirectory,
                            ["ExecutionContext:FunctionDirectory"] = executionContext.FunctionDirectory
                        }
                    )
                    .AddConfiguration(serviceProvider.GetService<IConfiguration>());
                });
                var exttype = typeof(IEnumerable<>);
                exttype = exttype.MakeGenericType(typeof(IWebHostBuilderExtension<>).MakeGenericType(aspNetCoreRunnerAttribute.Startup));
                var extensions = serviceProvider.GetService(exttype) as System.Collections.IEnumerable;
                foreach (var ext in extensions)
                {
                    var builderExtension = ext as IBuilderExtension;

                    builderExtension?.ConfigureWebHostBuilder(executionContext, builder, serviceProvider);

                }




                builder.UseStartup(aspNetCoreRunnerAttribute.Startup);



                builder.UseServer(this);

                //builder.Build().StartAsync().ContinueWith(task =>
                //{
                //    if (task.IsFaulted)
                //        _applicationSource.SetException(task.Exception);
                //});
            });

            genericbuilder.Build().StartAsync().ContinueWith(task =>
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

        public Task<IApplication> GetApplicationAsync()
        {
            return _applicationSource.Task;



        }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            this._applicationSource.SetResult(new ApplicationWrapper<TContext>(application));

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
