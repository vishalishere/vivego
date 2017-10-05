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
		private readonly IObservable<IPEndPoint[]> _seedsEndpointObservable;

		private readonly ISubject<(Node Node, bool Alive)[]> _clusterTopologyEventSubject =
			Subject.Synchronize(new Subject<(Node Node, bool Alive)[]>());

		static SeededLocalClusterProvider()
		{
			Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
		}

		public SeededLocalClusterProvider(IObservable<IPEndPoint[]> seedsEndpointObservable)
		{
			_seedsEndpointObservable = seedsEndpointObservable;
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
				.Select(tuples =>
				{
					ClusterTopologyEvent newTopology = new ClusterTopologyEvent(tuples
						.Where(tuple => !tuple.Node.PID.Address.Equals("nonhost"))
						.Select(tuple =>
						{
							Uri.TryCreate($"tcp://{tuple.Node.PID.Address}", UriKind.Absolute, out Uri uri);
							return new MemberStatus(tuple.Node.MemberId, uri.Host, uri.Port, tuple.Node.Kinds, tuple.Alive);
						})
						.ToArray());
					return newTopology;
				})
				.DistinctUntilChanged(new ClusterTopologyEventEqualityComparer())
				.Subscribe(clusterTopologyEvent => Actor.EventStream.Publish(clusterTopologyEvent), CancellationToken);

			Props props = Actor.FromProducer(() => new ClusterProviderIsAliveActor(_clusterTopologyEventSubject));
			PID isAlivePid = Actor.SpawnNamed(props, typeof(ClusterProviderIsAliveActor).FullName);
			int memberId = Process.GetCurrentProcess().Id;
			string[] kinds = Remote.GetKnownKinds();
			Node node = new Node
			{
				Kinds = {kinds},
				MemberId = memberId,
				PID = isAlivePid
			};

			_seedsEndpointObservable
				.Subscribe(seedEndpoints =>
				{
					isAlivePid.Tell(node);
					foreach (IPEndPoint seedEndpoint in seedEndpoints)
					{
						PID pid = EndpointToPid(seedEndpoint);
						pid.Tell(node);
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