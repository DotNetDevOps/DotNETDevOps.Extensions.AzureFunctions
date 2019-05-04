using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

    public class AspNetCoreRunnerServer<TWrapper,T> : IAspNetCoreServer where T : class
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

       
        




        public AspNetCoreRunnerServer(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment, Microsoft.Azure.WebJobs.ExecutionContext executionContext)
        {
 
            _applicationSource = new TaskCompletionSource<IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>>();

            var builder = new WebHostBuilder();


            if (hostingEnvironment.IsDevelopment())
            {
                var path = typeof(TWrapper).GetCustomAttribute<AspNetDevelopmentRelativePathAttribute>();
                if (path != null)
                {
                    builder.UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), path.Path));
                }
            }
            else
            {
                builder.UseContentRoot(executionContext.FunctionAppDirectory);
            }
            builder.ConfigureAppConfiguration((c, b) =>
            {
                var a = Directory.GetCurrentDirectory();

                //  b.SetBasePath(Directory.GetCurrentDirectory());

              //  if (!c.HostingEnvironment.IsProduction()) { b.AddUserSecrets("93CD8C24-88BA-4141-9E65-7E78FBDB6D94"); }

                //  b.AddInMemoryCollection(new[] { new KeyValuePair<string, string>(HostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) });

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
