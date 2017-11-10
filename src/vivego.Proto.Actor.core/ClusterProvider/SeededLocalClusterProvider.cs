using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Proto;
using Proto.Cluster;
using Proto.Remote;
using Proto.Router;

using vivego.core;
using vivego.Proto.Messages;

using Process = System.Diagnostics.Process;
using ProtosReflection = vivego.Proto.Messages.ProtosReflection;

namespace vivego.Proto.ClusterProvider
{
	public class SeededLocalClusterProvider : DisposableBase, IClusterProvider
	{
		private readonly IObservable<IEnumerable<IPEndPoint>> _seedsEndpointObservable;

		private readonly ISubject<(Node Node, bool Alive)[]> _clusterTopologyEventSubject =
			Subject.Synchronize(new Subject<(Node Node, bool Alive)[]>());

		static SeededLocalClusterProvider()
		{
			Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
		}

		public SeededLocalClusterProvider(IObservable<IEnumerable<IPEndPoint>> seedsEndpointObservable)
		{
			_seedsEndpointObservable = seedsEndpointObservable;
		}

		public Task UpdateMemberStatusValueAsync(IMemberStatusValue statusValue)
		{
			return Task.CompletedTask;
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

		public Task RegisterMemberAsync(string clusterName,
			string h,
			int p,
			string[] kinds,
			IMemberStatusValue statusValue,
			IMemberStatusValueSerializer serializer)
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
							return new MemberStatus(tuple.Node.MemberId, uri.Host, uri.Port, tuple.Node.Kinds, tuple.Alive, new SeededLocalClusterProviderMemberStatusValue(tuple.Node.MemberId));
						})
						.ToArray());

					return newTopology;
				})
				.DistinctUntilChanged(new ClusterTopologyEventEqualityComparer())
				.Subscribe(clusterTopologyEvent => Actor.EventStream.Publish(clusterTopologyEvent), CancellationToken);

			Props props = Actor.FromProducer(() => new ClusterProviderIsAliveActor(_clusterTopologyEventSubject));
			PID isAlivePid = Actor.SpawnNamed(props, typeof(ClusterProviderIsAliveActor).FullName);
			string memberId = $"{Environment.MachineName}_{Process.GetCurrentProcess().Id}";
			string[] kinds = Remote.GetKnownKinds();
			Node node = new Node
			{
				Kinds = {kinds},
				MemberId = memberId,
				PID = isAlivePid
			};

			_seedsEndpointObservable
				.Select(seedEndpoints =>
				{
					PID[] seedPids = seedEndpoints
						.Select(EndpointToPid)
						.AddToEnd(isAlivePid)
						.Distinct()
						.ToArray();
					Props broadcastProps = Router.NewBroadcastGroup(seedPids);
					return Actor.Spawn(broadcastProps);
				})
				.Scan((PID) null, (previous, @new) =>
				{
					previous?.Stop();
					return @new;
				})
				.Subscribe(broadcastPid => { broadcastPid.Tell(node); });
		}

		private static PID EndpointToPid(IPEndPoint ipEndPoint)
		{
			PID pid = new PID(ipEndPoint.ToString(), typeof(ClusterProviderIsAliveActor).FullName);
			return pid;
		}
	}

	internal class SeededLocalClusterProviderMemberStatusValue : IMemberStatusValue
	{
		private readonly string _memberId;

		public SeededLocalClusterProviderMemberStatusValue(string memberId)
		{
			_memberId = memberId;
		}

		public bool IsSame(IMemberStatusValue val)
		{
			if (val is SeededLocalClusterProviderMemberStatusValue statusValue)
			{
				return statusValue._memberId.Equals(_memberId);
			}

			return false;
		}
	}
}