using System.Reactive.Concurrency;
using System.Threading.Tasks;

using Proto;
using Proto.Cluster;
using Proto.Remote;

namespace vivego.ProtoBroker.Actors
{
	public class ClusterDistributedTimer : LocalDistributedTimer
	{
		static ClusterDistributedTimer()
		{
			Props props = Actor.FromProducer(() => new ClusterDistributedTimer(DefaultScheduler.Instance));
			Remote.RegisterKnownKind(nameof(ClusterDistributedTimer), props);
			//Serialization.RegisterFileDescriptor(Messages.BrokerReflection.Descriptor);
		}

		public ClusterDistributedTimer(IScheduler scheduler = null) : base(scheduler)
		{
		}

		private PID _distributedClusterActor;
		public override async Task Schedule(DeferMessage deferMessage)
		{
			if (_distributedClusterActor == null)
			{
				(PID, ResponseStatusCode) distributedClusterActor = await Cluster
					.GetAsync(nameof(ClusterDistributedTimer), "DistributedTimer")
					.ConfigureAwait(false);
				_distributedClusterActor = distributedClusterActor.Item1;
			}
			
			_distributedClusterActor.Tell(deferMessage);
		}
	}
}