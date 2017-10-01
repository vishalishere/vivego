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
		private PID[] _serverPids;
		private readonly ISubject<Alive[]> _clusterTopologyEventSubject = 
			Subject.Synchronize(new Subject<Alive[]>());

		static SeededLocalClusterProvider()
		{
			Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
		}

		public SeededLocalClusterProvider(
			params IPEndPoint[] seedsEndpoints)
		{
			_serverPids = seedsEndpoints
				.Select(serverEndPoint =>
				{
					PID pid = new PID(serverEndPoint.ToString(), typeof(ClusterProviderIsAliveActor).FullName);
					return pid;
				})
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
							Uri.TryCreate($"tcp://{a.AlivePID.Address}", UriKind.Absolute, out var uri);
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
				Kinds = { kinds },
				MemberId = memberId,
				AlivePID = isAlivePid
			};
			foreach (PID serverPiD in _serverPids.AddToEnd(isAlivePid))
			{
				serverPiD.Tell(alive);
			}
		}
	}
}