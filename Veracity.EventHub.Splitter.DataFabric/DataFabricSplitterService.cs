using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Veracity.EventHub.Abstraction;

namespace Veracity.EventHub.Splitter.DataFabric
{
    internal class DataFabricSplitterService : IHostedService
    {
        public class DiagnosticOperations
        {
            public const string ServiceStart = "Service_Start";
            public const string ServiceStop = "Service_Stop";
            public const string MessageProcess = "Message_Process";
            public const string DataFabricWrite = "DateFabric_Write";
        }

        private readonly DiagnosticListener _diagnostic;
        private readonly DataFabricSplitterConfig _config;
        private readonly IEventHub _eventHub;
        private ISubscription _subscription;

        public DataFabricSplitterService(IOptions<DataFabricSplitterConfig> config, IEventHub eventHub)
        {
            _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
            _eventHub = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
            _diagnostic = null;
        }

        public DataFabricSplitterService(IOptions<DataFabricSplitterConfig> config, IEventHub eventHub, DiagnosticListener diagnostic)
        {
            _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
            _eventHub = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
            _diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_diagnostic != null && _diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.ServiceStart))
                _diagnostic.Write(DiagnosticOperations.ServiceStart, _config);

            _subscription = _eventHub.Subscribe(_config.Namespace, async m=> await ProcessMessage(m, cancellationToken));

            return Task.CompletedTask;
        }

        private async Task ProcessMessage(EventMessage message, CancellationToken cancellationToken)
        {
            if (_diagnostic != null && _diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.MessageProcess))
                _diagnostic.Write(DiagnosticOperations.MessageProcess, message);

            await WriteToDataFabric(message.RouteKey, new MemoryStream(message.MessageBody), cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_diagnostic != null && _diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.ServiceStop))
                _diagnostic.Write(DiagnosticOperations.ServiceStop, _config);

            _subscription?.Unsubscribe();

            return Task.CompletedTask;
        }

        private async Task WriteToDataFabric(string routeKey, Stream stream, CancellationToken cancellationToken)
        {
            if (_diagnostic != null && _diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.MessageProcess))
                _diagnostic.Write(DiagnosticOperations.DataFabricWrite, _config);

            //Create a unique name for the blob
            var containerName = $"{_config.ContainerNamePrefix}-{routeKey}";

            var containerClient = new BlobContainerClient(_config.StorageConnectionString, containerName);

            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            
            var blobName =
                $"{_config.BlobNamePrefix}-{DateTime.UtcNow.ToString(string.IsNullOrEmpty(_config.BlobNameRollingTimeFormat) ? "yyyyMMdd" : _config.BlobNameRollingTimeFormat)}";

            var blobClient = containerClient.GetAppendBlobClient(blobName);

            var blobInfo =
                await blobClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            
            Console.WriteLine("Appending to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

            await blobClient.AppendBlockAsync(stream, null,
                new AppendBlobRequestConditions {IfMatch = blobInfo?.Value.ETag}, cancellationToken: cancellationToken);
        }
    }
}
