using System;
using System.Collections.Generic;

using Proto;
using Proto.Router.Routers;

namespace vivego.PublishSubscribe.ProtoActor.Route
{
	internal class ConsistentHashGroupRouterConfig : GroupRouterConfig
	{
		private readonly Func<string, uint> _hash;
		private readonly int _replicaCount;

		public ConsistentHashGroupRouterConfig(Func<string, uint> hash, int replicaCount, params PID[] routees)
		{
			if (replicaCount <= 0)
			{
				throw new ArgumentException("ReplicaCount must be greater than 0");
			}

			_hash = hash;
			_replicaCount = replicaCount;
			Routees = new HashSet<PID>(routees);
		}

		public override RouterState CreateRouterState()
		{
			return new ConsistentHashRouterState(_hash, _replicaCount);
		}
	}
}