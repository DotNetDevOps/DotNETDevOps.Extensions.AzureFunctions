using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {

        public static string GetVaultEnabledValue(this IConfiguration configuration, string key)
        {
            var connectionString = configuration.GetValue<string>(key);
            if (connectionString.StartsWith("@Microsoft.KeyVault"))
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var secret = keyVaultClient.GetSecretAsync(connectionString.Split(new[] { "SecretUri=" }, System.StringSplitOptions.RemoveEmptyEntries).Last().Trim(')')).GetAwaiter().GetResult();
                connectionString = secret.Value;
            }

            return connectionString;

        }
    }
    public static class KeyvaultExtensions
    {

        public static IServiceCollection ConfigureWithKeyVault<T>(this IServiceCollection services, Action<T, IConfiguration> configure)
           where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>(configure);
            services.AddSingleton<IConfigureOptions<T>, KeyVaultFetch<T>>();
            return services;
        }
        public static IServiceCollection ConfigureWithKeyVault<T>(this IServiceCollection services)
         where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);

            });
            services.AddSingleton<IConfigureOptions<T>, KeyVaultFetch<T>>();
            return services;
        }
    }
    public class KeyVaultFetch<T> : IConfigureOptions<T> where T : class
    {
        public void Configure(T options)
        {

            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();


            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));


            foreach (var prop in options.GetType().GetProperties())
            {
                var value = prop.GetValue(options);
                if (value is string strValue && strValue.StartsWith("@Microsoft.KeyVault"))
                {

                    var secret = keyVaultClient.GetSecretAsync(strValue.Split(new[] { "SecretUri=" }, System.StringSplitOptions.RemoveEmptyEntries).Last().Trim(')')).GetAwaiter().GetResult();
                    prop.SetValue(options, secret.Value);
                }

            }
        }
    }

}
