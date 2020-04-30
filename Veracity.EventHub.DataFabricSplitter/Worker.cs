using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veracity.EventHub.Abstraction;

namespace Veracity.EventHub.DataFabricSplitter
{
    public class Worker : BackgroundService
    {
        public const int PollInterval = 10;

        // public class DiagnosticOperations
        // {
        //     public const string ServiceStart = "Service_Start";
        //     public const string ServiceStop = "Service_Stop";
        //     public const string MessageProcess = "Message_Process";
        //     public const string DataFabricWrite = "DateFabric_Write";
        // }
        //
        // private readonly DiagnosticListener _diagnostic;

        private readonly DataFabricSplitterConfig _config;
        private readonly IEventHub _eventHub;
        private readonly ILogger<Worker> _logger;

        public Worker(IOptions<DataFabricSplitterConfig> config, IEventHub eventHub, ILogger<Worker> logger)
        {
            _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
            _eventHub = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started. (at: {time})", DateTimeOffset.UtcNow);

            if (!stoppingToken.IsCancellationRequested)
            {
                var subscription =
                    _eventHub.Subscribe(_config.Namespace, async m => await HandleEvent(m, stoppingToken));
                
                _logger.LogInformation("Worker started listening on EventHub¡£ (at: {time})", DateTimeOffset.UtcNow);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(PollInterval), stoppingToken);

                    _logger.LogInformation("Worker is still alive. (at: {time})", DateTimeOffset.UtcNow);
                }

                subscription.Unsubscribe();
            }

            _logger.LogInformation("Worker stopped. (at: {time})", DateTimeOffset.UtcNow);
        }

        private async Task HandleEvent(EventMessage message, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await PushToDataFabric(message.RoutingKey, new MemoryStream(message.MessageBody), stoppingToken);
        }

        private async Task PushToDataFabric(string routingKey, Stream stream, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            //Create a unique name for the blob
            var containerName = $"{_config.ContainerNamePrefix}-{routingKey}";

            var containerClient = new BlobContainerClient(_config.StorageConnectionString, containerName);

            await containerClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            stoppingToken.ThrowIfCancellationRequested();

            var blobName =
                $"{_config.BlobNamePrefix}-{DateTime.UtcNow.ToString(string.IsNullOrEmpty(_config.BlobNameRollingTimeFormat) ? "yyyyMMdd" : _config.BlobNameRollingTimeFormat)}";

            var blobClient = containerClient.GetAppendBlobClient(blobName);

            var blobInfo =
                await blobClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Appending to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

            await blobClient.AppendBlockAsync(stream, null,
                new AppendBlobRequestConditions { IfMatch = blobInfo?.Value.ETag }, cancellationToken: stoppingToken);
        }
    }
}
