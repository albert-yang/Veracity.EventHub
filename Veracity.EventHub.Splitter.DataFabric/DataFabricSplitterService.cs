using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veracity.EventHub.Abstraction;

namespace Veracity.EventHub.DataFabricArchiver
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

        public DataFabricSplitterService(DiagnosticListener diagnostic, IOptions<DataFabricSplitterConfig> config, IEventHub eventHub)
        {
            _diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
            _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
            _eventHub = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.ServiceStart))
                _diagnostic.Write(DiagnosticOperations.ServiceStart, _config);

            _subscription = _eventHub.Subscribe(_config.Namespace, async m=> await ProcessMessage(m, cancellationToken));

            return Task.CompletedTask;
        }

        private async Task ProcessMessage(EventMessage message, CancellationToken cancellationToken)
        {
            if (_diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.MessageProcess))
                _diagnostic.Write(DiagnosticOperations.MessageProcess, message);

            await WriteToDataFabric(new MemoryStream(message.MessageBody), cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.ServiceStop))
                _diagnostic.Write(DiagnosticOperations.ServiceStop, _config);

            _subscription?.Unsubscribe();

            return Task.CompletedTask;
        }

        private async Task WriteToDataFabric(Stream stream, CancellationToken cancellationToken)
        {
            if (_diagnostic.IsEnabled() && _diagnostic.IsEnabled(DiagnosticOperations.MessageProcess))
                _diagnostic.Write(DiagnosticOperations.DataFabricWrite, _config);

            //Create a unique name for the blob
            var containerName = _config.ContainerName;

            var blobName =
                $"{_config.BlobNamePrefix}-{DateTime.UtcNow.ToString(string.IsNullOrEmpty(_config.BlobNameRollingTimeFormat) ? "yyyyMMdd" : _config.BlobNameRollingTimeFormat)}";

            var blobServiceClient = new AppendBlobClient(_config.StorageConnectionString, containerName, blobName);

            BlobContentInfo blobInfo =
                await blobServiceClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            Console.WriteLine("Appending to Blob storage as blob:\n\t {0}\n", blobServiceClient.Uri);

            await blobServiceClient.AppendBlockAsync(stream, null,
                new AppendBlobRequestConditions {IfMatch = blobInfo.ETag}, cancellationToken: cancellationToken);
        }
    }
}
