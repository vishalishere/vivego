using System;
using System.Collections.Generic;

using Proto;
using Proto.Router;
using Proto.Router.Routers;

namespace vivego.PublishSubscribe.ProtoActor.Route
{
	internal class ConsistentHashRouterState : RouterState
	{
		private readonly Func<string, uint> _hash;
		private readonly int _replicaCount;
		private HashRing _hashRing;
		private Dictionary<string, PID> _routeeMap;

		public ConsistentHashRouterState(Func<string, uint> hash, int replicaCount)
		{
			_hash = hash;
			_replicaCount = replicaCount;
		}

		public override HashSet<PID> GetRoutees()
		{
			return new HashSet<PID>(_routeeMap.Values);
		}

		public override void SetRoutees(HashSet<PID> routees)
		{
			_routeeMap = new Dictionary<string, PID>();
			List<string> nodes = new List<string>();
			foreach (PID pid in routees)
			{
				string nodeName = pid.ToShortString();
				nodes.Add(nodeName);
				_routeeMap[nodeName] = pid;
			}

			_hashRing = new HashRing(nodes, _hash, _replicaCount);
		}

		public override void RouteMessage(object message)
		{
			(object message, PID sender, MessageHeader headers) env = MessageEnvelope.Unwrap(message);
			if (env.message is Message message1)
			{
				string node = _hashRing.GetNode("");//message1.HashBy);
				PID routee = _routeeMap[node];
				routee.Tell(message);
			}
			else
			{
				throw new NotSupportedException($"Message of type '{message.GetType().Name}' does not implement IHashable");
			}
		}
	}
}