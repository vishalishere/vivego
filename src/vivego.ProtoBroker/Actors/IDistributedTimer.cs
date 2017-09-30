using System.Threading.Tasks;

namespace vivego.ProtoBroker.Actors
{
	public interface IDistributedTimer
	{
		Task Schedule(DeferMessage deferMessage);
	}
}