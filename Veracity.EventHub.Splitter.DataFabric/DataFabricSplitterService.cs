using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Hosting;

namespace Veracity.EventHub.DataFabricArchiver
{
    internal class DataFabricSplitterService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        static object _locker = new object();
        static void ArchiveToDataFabric(string routeKey, Stream stream)
        {
            lock (_locker)
            {
                //Create a unique name for the container
                var containerName = $"{routeKey}_archive";

                //
                var blobName = $"archive_{DateTime.UtcNow.ToString("yyyyMMdd")}";

                var blobServiceClient = new AppendBlobClient(
                    @"https://we1dnvglpstgcus0000ep9eh.blob.core.windows.net/event-hub-archiveba917958-1042-4b11-85d2-133a80616c1d?sv=2018-03-28&sr=c&sig=kN8yuGxvWBlEUKhqGTP4gLQyMB2AqTZ1g8o3ezTkdYU%3D&st=2020-01-17T06%3A55%3A47Z&se=2020-07-15T07%3A55%3A41Z&sp=w",
                    containerName, blobName);

                BlobContentInfo blobInfo = blobServiceClient.CreateIfNotExists();

                Console.WriteLine("Appending to Blob storage as blob:\n\t {0}\n", blobServiceClient.Uri);

                blobServiceClient.AppendBlock(stream, null,
                    new AppendBlobRequestConditions { IfMatch = blobInfo.ETag });
            }
        }
    }
}
