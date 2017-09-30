using System.Collections.Generic;
using System.Linq;

using Proto.Cluster;

namespace vivego.Proto.ClusterProvider
{
	public class ClusterTopologyEventEqualityComparer : IEqualityComparer<ClusterTopologyEvent>
	{
		public bool Equals(ClusterTopologyEvent x, ClusterTopologyEvent y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			if (x.Statuses.Count != y.Statuses.Count)
			{
				return false;
			}

			return x.Statuses
				.OrderBy(memberStatus => memberStatus.Host)
				.ThenBy(memberStatus => memberStatus.Port)
				.SequenceEqual(y.Statuses
					.OrderBy(memberStatus => memberStatus.Host)
					.ThenBy(memberStatus => memberStatus.Port), new MemberStatusEqualityComparer());
		}

		public int GetHashCode(ClusterTopologyEvent obj)
		{
			return obj.Statuses.Aggregate(obj.Statuses.GetHashCode(),
				(hashCode, memberStatus) => (hashCode * 397) ^ memberStatus.GetHashCode());
		}
	}

	public sealed class MemberStatusEqualityComparer : IEqualityComparer<MemberStatus>
	{
		public bool Equals(MemberStatus x, MemberStatus y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			return x.MemberId == y.MemberId
				&& string.Equals(x.Host, y.Host)
				&& x.Port == y.Port
				&& Equals(x.Kinds, y.Kinds)
				&& x.Alive == y.Alive;
		}

		public int GetHashCode(MemberStatus obj)
		{
			unchecked
			{
				int hashCode = obj.MemberId.GetHashCode();
				hashCode = (hashCode * 397) ^ (obj.Host != null ? obj.Host.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ obj.Port;
				hashCode = (hashCode * 397) ^ (obj.Kinds != null ? obj.Kinds.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ obj.Alive.GetHashCode();
				return hashCode;
			}
		}
	}
}