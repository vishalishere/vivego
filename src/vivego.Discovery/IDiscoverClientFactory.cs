using System;
using System.Net;

namespace vivego.Discovery.Abstactions
{
	public interface IDiscoverClientFactory
	{
		IObservable<IPEndPoint[]> MakeObservable();
	}
}