using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Bindings;


namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class AspNetCoreExtension : IExtensionConfigProvider
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
        }

        private Task<IAspNetCoreRunner> Factory(AspNetCoreRunnerAttribute arg1, ValueBindingContext arg2)
        {

            return Task.FromResult(new AspNetCoreRunner(this.serviceProvider,arg1,arg2) as IAspNetCoreRunner);

         //    arg2.FunctionContext.MethodName
         // return serviceProvider.GetService(typeof(IAspNetCoreRunner<>).MakeGenericType(typeof()))
        }
    }
}
