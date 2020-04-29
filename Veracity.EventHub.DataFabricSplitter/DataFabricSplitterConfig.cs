namespace Veracity.EventHub.DataFabricSplitter
{
    public class DataFabricSplitterConfig
    {
        public const string KubeConfigMapName = "appsetting-from-configmap";

        public const string KubeSecretName = "appsetting-from-secret";

        public const string KubeNamespace = "default";

        public string EventHubEndpoint { get; set; }

        public string Namespace { get; set; }

        public string ContainerNamePrefix { get; set; }

        public string BlobNamePrefix { get; set; }

        public string BlobNameRollingTimeFormat { get; set; }

        public string StorageConnectionString { get; set; }
    }
}