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

using vivego.core;
using vivego.Proto.Messages;

using Process = System.Diagnostics.Process;
using ProtosReflection = vivego.Proto.Messages.ProtosReflection;

namespace vivego.Proto.ClusterProvider
{
	public class StaticClusterProvider : DisposableBase, IClusterProvider
	{
		private readonly TimeSpan _livenessPublishInterval;
		private PID[] _serverPids;
		private readonly ISubject<IDictionary<string, Alive>> _clusterTopologyEventSubject = 
			Subject.Synchronize(new Subject<IDictionary<string, Alive>>());

		static StaticClusterProvider()
		{
			Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
		}

		public StaticClusterProvider(
			TimeSpan livenessPublishInterval,
			params IPEndPoint[] protoActorServerEndpoints)
		{
			_livenessPublishInterval = livenessPublishInterval;

			Actor.SpawnNamed(Actor.FromProducer(() => new ClusterProviderIsAliveActor(_clusterTopologyEventSubject)),
				typeof(ClusterProviderIsAliveActor).FullName);

			_serverPids = protoActorServerEndpoints
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
						.Select(alive => new MemberStatus(alive.Value.MemberId, alive.Value.Host, alive.Value.Port, alive.Value.Kinds, true))
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

			int memberId = Process.GetCurrentProcess().Id;
			Uri.TryCreate($"tcp://{Remote.EndpointManagerPid.Address}", UriKind.Absolute, out Uri uri);
			string host = uri.Host;
			int port = uri.Port;
			Observable
				.Interval(_livenessPublishInterval)
				.Merge(Observable.Return(0L)) // Trigger immidiately first time
				.Subscribe(_ =>
				{
					string[] kinds = Remote.GetKnownKinds();
					Alive alive = new Alive
					{
						Kinds = {kinds},
						MemberId = memberId,
						Host = host,
						Port = port
					};
					foreach (PID serverPiD in _serverPids)
					{
						serverPiD.Tell(alive);
					}
				}, CancellationToken);
		}
	}

	internal class ClusterProviderIsAliveActor : IActor
	{
		private readonly IObserver<IDictionary<string, Alive>> _observer;
		private readonly IDictionary<string, Alive> _watchList = new Dictionary<string, Alive>();

		public ClusterProviderIsAliveActor(IObserver<IDictionary<string, Alive>> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Alive alive:
					string key = $"{alive.Host}:{alive.Port}";
					bool removed = _watchList.Remove(key);
					_watchList.Add(key, alive);
					if (!removed)
					{
						context.Watch(new PID($"{alive.Host}:{alive.Port}", typeof(ClusterProviderIsAliveActor).FullName));
					}

					_observer.OnNext(_watchList);
					break;
				case Terminated terminated:
					_watchList.Remove(terminated.Who.Address);
					_observer.OnNext(_watchList);
					break;
			}

			return Task.CompletedTask;
		}
	}
}