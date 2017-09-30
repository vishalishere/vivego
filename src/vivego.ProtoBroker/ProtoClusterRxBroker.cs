using System;
using System.Reactive.Subjects;

using Proto;
using Proto.Cluster;
using Proto.Remote;

using vivego.core;
using vivego.ProtoBroker.Actors;
using vivego.ProtoBroker.Messages;

namespace vivego.ProtoBroker
{
	internal class ProtoClusterRxBroker : IRxBroker
	{
		private readonly PID _brokerPid;

		public ProtoClusterRxBroker(IClusterProvider clusterProvider)
		{
			Props props = Actor.FromProducer(() => new BrokerMasterActor());
			Remote.RegisterKnownKind(nameof(BrokerMasterActor), props);
			Serialization.RegisterFileDescriptor(BrokerReflection.Descriptor);

			Remote.Start("127.0.0.1", PortUtils.FindAvailablePort());
			//Cluster.Start("MyCluster", clusterProvider);

			//PID brokerMasterPid = Cluster.GetAsync("Master", nameof(BrokerMasterActor)).Result;
			//Cluster.GetAsync("Master2", nameof(BrokerMasterActor)).Wait();
			//Cluster.GetAsync("Master" + Guid.NewGuid(), nameof(BrokerMasterActor)).Wait();

			//props = Actor.FromProducer(() => new BrokerActor(brokerMasterPid));
			//_brokerPid = Actor.SpawnNamed(props, "Broker");
		}

		public ISubject<T> MakeSubject<T>(string topic,
			string group = null)
		{
			return new ProtoBrokerSubject<T>(_brokerPid, topic, group);
		}
	}
}