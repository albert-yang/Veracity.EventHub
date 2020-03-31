namespace Veracity.EventHub.Splitter.DataFabric
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