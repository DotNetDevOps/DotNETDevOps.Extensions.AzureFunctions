using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Extensions;

namespace DotNETDevOps.Extensions.AzureFunctions
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : (value.Substring(0, maxLength-3) + "...");
        }
    }

    public class HttpContextAccessorStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> nextBuilder)
        {
            return builder =>
            {
                builder.Use((ctx, next) =>
                {
                    var httpcontextacccess = ctx.RequestServices.GetService<IHttpContextAccessor>();
                    if (httpcontextacccess != null)
                    {
                        httpcontextacccess.HttpContext = ctx;
                    }

                  
                   // var RequestTelemetry = serviceType.Assembly.GetType("Microsoft.ApplicationInsights.DataContracts.RequestTelemetry");

                    return next();

                });
                builder.Use((ctx, next) =>
                {
                    try
                    {
                        //TODO Performance improve
                        var a = ctx.RequestServices.GetService<TeleetryConfigurationProvider>();
                        var request = ctx.Features[a.GetRequestTelemetryType()];
                        if (request != null)
                        {
                            var propertiesInfo = request.GetType().GetProperty("Properties");
                            var properties = propertiesInfo.GetValue(request) as IDictionary<string, string>;
                            properties["HttpPathBase"] = ctx.Request.PathBase.Value ?? string.Empty;
                            properties["HttpPath"] = ctx.Request.Path.Value ?? "/";

                            var url = new Uri(ctx.Request.GetDisplayUrl());
                            properties["DisplayUrl"] = url.AbsoluteUri;
                            properties["DisplayHost"] = url.Host;

                             
                            RmoveIfExists(properties, "TriggerReason");
                            RmoveIfExists(properties, "FullName");

                            foreach (var header in ctx.Request.Headers)
                            {
                                properties[header.Key] = string.Join(" ", header.Value).Truncate(80);
                            }
                            RmoveIfExists(properties, "TriggerReason");
                            RmoveIfExists(properties, "FullName");
                            RmoveIfExists(properties, "X-AppService-Proto");
                            

                        }
                    }
                    catch (Exception)
                    {

                    }

                    return next();
                });

                nextBuilder(builder);
               
            };

        }

        private static void RmoveIfExists(IDictionary<string, string> properties, string x)
        {
            if (properties.ContainsKey(x))
                properties.Remove(x);
        }
    }
}
