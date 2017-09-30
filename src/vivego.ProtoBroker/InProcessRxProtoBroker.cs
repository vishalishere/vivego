using System.Reactive.Subjects;

using Proto;

using vivego.ProtoBroker.Actors;

namespace vivego.ProtoBroker
{
	internal class ProtoInProcessRxBroker : IRxBroker
	{
		private readonly PID _brokerPid;

		public ProtoInProcessRxBroker()
		{
			Props brokerMasterActorProps = Actor.FromProducer(() => new BrokerMasterActor());
			PID brokerMasterActor = Actor.SpawnNamed(brokerMasterActorProps, "BrokerMaster");
			Props props = Actor.FromProducer(() => new BrokerActor(brokerMasterActor));
			_brokerPid = Actor.SpawnPrefix(props, "Broker");
		}

		public ISubject<T> MakeSubject<T>(string topic,
			string group = null)
		{
			return new ProtoBrokerSubject<T>(_brokerPid, topic, group);
		}
	}
}