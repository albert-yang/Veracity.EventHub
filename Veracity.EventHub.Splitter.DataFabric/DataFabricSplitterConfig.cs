using System;

namespace Veracity.EventHub.DataFabricArchiver
{
    public class DataFabricSplitterConfig
    {
        public string Namespace { get; set; }

        public string ContainerName { get; internal set; }

        public string BlobNamePrefix { get; set; }

        public string BlobNameRollingTimeFormat { get; internal set; }

        public string StorageConnectionString { get; set; }
    }
}