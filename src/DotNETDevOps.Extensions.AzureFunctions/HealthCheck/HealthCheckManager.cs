using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DotNETDevOps.Extensions.AzureFunctions.HealthCheck
{
    
    public class HealthCheckManager
    {
        private readonly IEnumerable<IExtensionConfigProvider> extensions;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger logger;

        public HealthCheckManager(IEnumerable<IExtensionConfigProvider> extensions, IHttpClientFactory httpClientFactory, ILogger<HealthCheckManager> logger)
        {
            this.extensions = extensions;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        public string Url
        {
            get
            {
                var url = this.extensions.OfType<AspNetCoreExtension>().First().uri;
                return url.GetLeftPart(UriPartial.Authority);
            }
        }

        public async Task Healthcheck()
        {
            //    var uri = this.extensions.OfType<test>().First().uri.AbsoluteUri;//.Replace("http://", "https://");
            var url = Url + "/.well-known/live";
            try
            {
                var resp = await httpClientFactory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogWarning("{url} is not live: {status}", url, resp.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("{url} is not live", url);
            }
        }

    }
}
