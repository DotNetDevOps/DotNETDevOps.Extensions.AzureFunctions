using DotNETDevOps.Extensions.AzureFunctions.HealthCheck;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

[assembly: WebJobsStartup(typeof(DotNETDevOps.Extensions.AzureFunctions.ExtensionStartup))]

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public class TeleetryConfigurationProvider
    {
        public Type serviceType;
        
        public TeleetryConfigurationProvider(Type serviceType)
        {
            this.serviceType = serviceType;
        }

        internal void Initialize(IServiceProvider serviceProvider)
        {
            if (serviceType != null)
            {

                var tc = serviceProvider.GetService(serviceType);


                var chain = serviceType.GetProperty("TelemetryProcessorChainBuilder").GetValue(tc);
                var ITelemetryProcessor = serviceType.Assembly.GetType("Microsoft.ApplicationInsights.Extensibility.ITelemetryProcessor");
                var funcType = typeof(Func<,>).MakeGenericType(ITelemetryProcessor, ITelemetryProcessor);
                var methodInfo = this.GetType().GetMethod("Test").MakeGenericMethod(ITelemetryProcessor);


                //  var func = Delegate.CreateDelegate(funcType, methodInfo);
                // var f = (object o) => methodInfo.Invoke(this, new[] { o });
                chain.GetType().GetMethod("Use").Invoke(chain, new object[] { this.GetType().GetMethod("GetMethod").MakeGenericMethod(ITelemetryProcessor).Invoke(this, null) });
                chain.GetType().GetMethod("Build").Invoke(chain, new object[] { });
            }
        }
        public Func<T,T> GetMethod<T>()
        {
            return (T a) => Test<T>(a);
        }
        public T Test<T>(T arg)
        {
            TypeBuilder tb = GetTypeBuilder("FixNameProcessor");

            var t = tb.CreateType();
            
            return (T)Activator.CreateInstance(t, new object[] { (T)arg });
        }
        public static void FixRequest(dynamic request)
        {
            if(request.GetType().Name == "RequestTelemetry")
            {
              //  var Properties = o.GetType().GetProperty("Properties").GetValue(o) as IDictionary<string,string>;
                request.Name = $"{request.Properties["HttpMethod"]} {request.Properties["HttpPath"]}";
                request.Context.Operation.Name = request.Name;
            }
        }
        private  TypeBuilder GetTypeBuilder(string name)
        {
            var ITelemetryProcessor = serviceType.Assembly.GetType("Microsoft.ApplicationInsights.Extensibility.ITelemetryProcessor");
            var ITelemetry = serviceType.Assembly.GetType("Microsoft.ApplicationInsights.Channel.ITelemetry");
            var RequestTelemetry = serviceType.Assembly.GetType("Microsoft.ApplicationInsights.DataContracts.RequestTelemetry");

            var typeSignature = name;
            var an = new AssemblyName(typeSignature);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            tb.AddInterfaceImplementation(ITelemetryProcessor);

            var field = tb.DefineField("_next", ITelemetryProcessor, FieldAttributes.Private);
            var someMethod = field.FieldType.GetMethod("Process");

            var process = tb.DefineMethod("Process", MethodAttributes.Public| MethodAttributes.Virtual, null, parameterTypes: new Type[] { ITelemetry });

            var ilGen = process.GetILGenerator();
           // ilGen.Emit(OpCodes.Ldarg_0);
           
            //  ilGen.Emit(OpCodes.Ldarg_1);
            //  
            var b = this.GetType().GetMethod("FixRequest", BindingFlags.Static| BindingFlags.Public);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Call,b );


            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, field);
            ilGen.Emit(OpCodes.Ldarg_1);

            ilGen.Emit(OpCodes.Callvirt, someMethod);
            ilGen.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(process, someMethod);

            ConstructorBuilder constructor = tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { ITelemetryProcessor});

           
            var setIL = constructor.GetILGenerator();
             
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Call, typeof(object).GetConstructors().Single());
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, field);
            setIL.Emit(OpCodes.Ret);

          

           
           

            return tb;
        }
    }
    internal class ExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton(new TeleetryConfigurationProvider(builder.Services.FirstOrDefault(t => t.ServiceType.Name.Contains("TelemetryConfiguration"))?.ServiceType));
            builder.AddExtension<AspNetCoreExtension>();
            builder.Services.AddSingleton<HealthCheckManager>();
            builder.Services.AddHttpClient();
        }
    }
}
