using System;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Veracity.EventHub.Abstraction;
using Veracity.EventHub.RabbitMQ;

namespace Veracity.EventHub.Splitter.DataFabric
{
    internal class Program
    {
        public const string KubeConfigMapName = "appsetting-from-configmap";
        //public const string KubeSecretName = nameof(KubeSecretName);
        public const string KubeNamespace = "default";

        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
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
                            cb.AddKubeConfigMap(KubeClientOptions.FromPodServiceAccount(),
                                config[KubeConfigMapName],
                                config[KubeNamespace],
                                reloadOnChange: false);
                        // .AddKubeSecret(
                        //     KubeClientOptions.FromPodServiceAccount(),
                        //     config[KubeSecretName],
                        //     config[KubeNamespace],
                        //     reloadOnChange: true);
                    }

                    cb.AddEnvironmentVariables();

                    if (args != null)
                    {
                        cb.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddOptions()
                        .Configure<DataFabricSplitterConfig>(hostContext.Configuration);
                    
                    services
                        .AddTransient<IEventHub>(sp => 
                           new RabbitMQHub(new ConnectionFactory
                           {
                               Endpoint = new AmqpTcpEndpoint(new Uri(hostContext.Configuration[nameof(DataFabricSplitterConfig.EventHubEndpoint)])),
                               DispatchConsumersAsync = true
                           }))
                        .AddHostedService<DataFabricSplitterService>();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            await builder.RunConsoleAsync();
        }
    }
}