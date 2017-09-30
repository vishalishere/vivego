using Proto.Cluster;

namespace vivego.ProtoBroker
{
	public class ProtoBrokerFactory
	{
		public static IRxBroker InProcess() => new ProtoInProcessRxBroker();

		public static IRxBroker Cluster(IClusterProvider clusterProvider) => new ProtoClusterRxBroker(clusterProvider);
	}
}