using System;
using System.Reactive.Subjects;

using vivego.core;

namespace vivego.PublishSubscribe.Topology
{
	public class StaticClusterTopologyObservable : DisposableBase, IObservable<Uri[]>
	{
		private readonly ReplaySubject<Uri[]> _replaySubject = new ReplaySubject<Uri[]>(1);

		public StaticClusterTopologyObservable(params Uri[] serverUris)
		{
			_replaySubject.OnNext(serverUris);
			RegisterDisposable(_replaySubject);
		}

		public IDisposable Subscribe(IObserver<Uri[]> observer)
		{
			return _replaySubject.Subscribe(observer);
		}
	}
}