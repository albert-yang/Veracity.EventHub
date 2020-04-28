using System.Threading.Tasks;
using KubeClient;
using KubeClient.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Veracity.EventHub.Abstraction;
using Veracity.EventHub.RabbitMQ;

namespace Veracity.EventHub.Splitter.DataFabric
{
    internal class Program
    {
        public const string KubeConfigMapName = nameof(KubeConfigMapName);
        public const string KubeSecretName = nameof(KubeSecretName);
        public const string KubeNamespace = nameof(KubeNamespace);

        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, cb) =>
                {
                    cb.AddJsonFile("appsettings.json", false, false)
                        .AddJsonFile(
                            $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", 
                            true, false);

                    if (hostContext.HostingEnvironment.IsDevelopment())
                        cb.AddUserSecrets<Program>();
                    else
                    {
                        var config = cb.Build();

                        if (!hostContext.HostingEnvironment.IsDevelopment())
                            cb.AddEnvironmentVariables()
                                .AddKubeConfigMap(KubeClientOptions.FromPodServiceAccount(),
                                    config[KubeConfigMapName],
                                    config[KubeNamespace],
                                    reloadOnChange: true)
                                .AddKubeSecret(
                                    KubeClientOptions.FromPodServiceAccount(),
                                    config[KubeSecretName],
                                    config[KubeNamespace],
                                    reloadOnChange: true);
                    }

                    if (args != null)
                    {
                        cb.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    if (hostContext.HostingEnvironment.IsDevelopment())
                        services.Configure<DataFabricSplitterConfig>(hostContext.Configuration.GetSection(nameof(DataFabricSplitterConfig)));
                    else
                        services.Configure<DataFabricSplitterConfig>(hostContext.Configuration);

                    services.AddScoped<IEventHub>(sp => new RabbitMQHub(new ConnectionFactory{ HostName = ""})
                    services.AddSingleton<IHostedService, DataFabricSplitterService>();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            await builder.RunConsoleAsync();
        }
    }
}