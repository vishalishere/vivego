using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;

namespace vivego.Orleans.Providers.Membership
{
	public class MembershipProvider : IMembershipTable
	{
		public Task InitializeMembershipTable(bool tryInitTableVersion)
		{
			throw new System.NotImplementedException();
		}

		public Task DeleteMembershipTableEntries(string deploymentId)
		{
			throw new System.NotImplementedException();
		}

		public Task<MembershipTableData> ReadRow(SiloAddress key)
		{
			throw new System.NotImplementedException();
		}

		public Task<MembershipTableData> ReadAll()
		{
			throw new System.NotImplementedException();
		}

		public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
		{
			throw new System.NotImplementedException();
		}

		public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
		{
			throw new System.NotImplementedException();
		}

		public Task UpdateIAmAlive(MembershipEntry entry)
		{
			throw new System.NotImplementedException();
		}
	}

	//public class MembershipEntryActor : IActor
	//{
	//	private MembershipEntry _state;

	//	public Task ReceiveAsync(IContext context)
	//	{
	//		throw new System.NotImplementedException();
	//	}
	//}
}