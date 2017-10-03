using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Proto;
using Proto.Cluster;
using Proto.Remote;

using vivego.core;
using vivego.Proto.Messages;

using Process = System.Diagnostics.Process;
using ProtosReflection = vivego.Proto.Messages.ProtosReflection;

namespace vivego.Proto.ClusterProvider
{
	public class SeededLocalClusterProvider : DisposableBase, IClusterProvider
	{
		private readonly TimeSpan _broadcastInterval;

		private readonly ISubject<Alive[]> _clusterTopologyEventSubject =
			Subject.Synchronize(new Subject<Alive[]>());

		private readonly IPEndPoint[] _seedsEndpoints;
		private PID[] _serverPids;

		static SeededLocalClusterProvider()
		{
			Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
		}

		public SeededLocalClusterProvider(params IPEndPoint[] seedsEndpoints)
			: this(TimeSpan.FromSeconds(60), seedsEndpoints)
		{
		}

		public SeededLocalClusterProvider(TimeSpan broadcastInterval,
			params IPEndPoint[] seedsEndpoints)
		{
			_broadcastInterval = broadcastInterval;
			_seedsEndpoints = seedsEndpoints;
			_serverPids = seedsEndpoints
				.Select(EndpointToPid)
				.ToArray();
		}

		public Task DeregisterMemberAsync()
		{
			return Task.CompletedTask;
		}

		public Task Shutdown()
		{
			Dispose();
			return Task.CompletedTask;
		}

		public Task RegisterMemberAsync(string clusterName, string h, int p, string[] kinds)
		{
			return Task.CompletedTask;
		}

		public void MonitorMemberStatusChanges()
		{
			_clusterTopologyEventSubject
				.Select(alives =>
				{
					ClusterTopologyEvent newTopology = new ClusterTopologyEvent(alives
						.Where(a => !a.AlivePID.Address.Equals("nonhost"))
						.Select(a =>
						{
							Uri.TryCreate($"tcp://{a.AlivePID.Address}", UriKind.Absolute, out Uri uri);
							return new MemberStatus(a.MemberId, uri.Host, uri.Port, a.Kinds, true);
						})
						.ToArray());
					return newTopology;
				})
				.DistinctUntilChanged(new ClusterTopologyEventEqualityComparer())
				.Subscribe(clusterTopologyEvent =>
				{
					_serverPids = clusterTopologyEvent
						.Statuses
						.Select(serverEndPoint =>
						{
							PID pid = new PID(serverEndPoint.Address, typeof(ClusterProviderIsAliveActor).FullName);
							return pid;
						})
						.ToArray();
					Actor.EventStream.Publish(clusterTopologyEvent);
				}, CancellationToken);

			Props props = Actor.FromProducer(() => new ClusterProviderIsAliveActor(_clusterTopologyEventSubject));
			PID isAlivePid = Actor.SpawnNamed(props, typeof(ClusterProviderIsAliveActor).FullName);
			int memberId = Process.GetCurrentProcess().Id;
			string[] kinds = Remote.GetKnownKinds();
			Alive alive = new Alive
			{
				Kinds = {kinds},
				MemberId = memberId,
				AlivePID = isAlivePid
			};
			Observable
				.Interval(_broadcastInterval)
				.Merge(Observable.Return(0L))
				.Subscribe(_ =>
				{
					foreach (PID serverPiD in _serverPids
						.AddToEnd(_seedsEndpoints.Select(EndpointToPid))
						.AddToEnd(isAlivePid)
						.Distinct())
					{
						serverPiD.Tell(alive);
					}
				}, CancellationToken);
		}

		private static PID EndpointToPid(IPEndPoint ipEndPoint)
		{
			PID pid = new PID(ipEndPoint.ToString(), typeof(ClusterProviderIsAliveActor).FullName);
			return pid;
		}
	}
}