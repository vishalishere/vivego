using System;
using System.Net;

namespace vivego.Discovery
{
	public interface IDiscoverClientFactory
	{
		IObservable<IPEndPoint[]> MakeObservable();
	}
}