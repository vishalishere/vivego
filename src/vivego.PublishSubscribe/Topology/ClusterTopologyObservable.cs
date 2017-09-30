using System;

namespace vivego.PublishSubscribe.Topology
{
	public class ClusterTopologyObservable : IObservable<Uri>
	{
		public IDisposable Subscribe(IObserver<Uri> observer)
		{
			throw new NotImplementedException();
		}
	}
}