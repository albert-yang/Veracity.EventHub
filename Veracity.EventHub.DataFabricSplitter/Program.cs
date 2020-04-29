using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Veracity.EventHub.DataFabricSplitter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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
                                config[DataFabricSplitterConfig.KubeConfigMapName],
                                config[DataFabricSplitterConfig.KubeNamespace],
                                reloadOnChange: false);
                        // .AddKubeSecret(
                        //     KubeClientOptions.FromPodServiceAccount(),
                        //     config[DataFabricSplitterConfig.KubeSecretName],
                        //     config[DataFabricSplitterConfig.KubeNamespace],
                        //     reloadOnChange: false);
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
                        .AddHostedService<Worker>();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging
                        .AddConfiguration(hostingContext.Configuration.GetSection("Logging"))
                        .AddConsole()
                        .AddDebug();
                });
    }
}
