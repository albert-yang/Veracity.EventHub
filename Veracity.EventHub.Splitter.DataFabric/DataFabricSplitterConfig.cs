namespace Veracity.EventHub.Splitter.DataFabric
{
    public class DataFabricSplitterConfig
    {
        public string EventHubEndpoint { get; set; }

        public string Namespace { get; set; }

        public string ContainerNamePrefix { get; set; }

        public string BlobNamePrefix { get; set; }

        public string BlobNameRollingTimeFormat { get; set; }

        public string StorageConnectionString { get; set; }
    }
}