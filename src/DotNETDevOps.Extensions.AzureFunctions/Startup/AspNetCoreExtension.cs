using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs;
using System.Net.Http;
using System.Threading;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreExtension : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
    {
        private readonly IServiceProvider serviceProvider;

        public AspNetCoreExtension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<AspNetCoreRunnerAttribute>();

            rule.BindToInput(Factory);

            uri = context.GetWebhookHandler();
        }

        private Task<IAspNetCoreRunner> Factory(AspNetCoreRunnerAttribute arg1, ValueBindingContext arg2)
        {

            return Task.FromResult(new AspNetCoreRunner(this.serviceProvider,arg1,arg2) as IAspNetCoreRunner);

         
        }

        public Uri uri { get; private set; }

        public Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

       
    }
}
