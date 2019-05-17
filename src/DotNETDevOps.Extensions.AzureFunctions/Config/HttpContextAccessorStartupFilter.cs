using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DotNETDevOps.Extensions.AzureFunctions
{
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

                    return next();

                });
                nextBuilder(builder);
            };

        }
    }
}
