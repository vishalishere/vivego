using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;

namespace vivego.Discovery
{
	public static class DiscoverClientFactoryExtensions
	{
		public static IPEndPoint[] DiscoverEndPoints(this IDiscoverClientFactory discoverClientFactory)
		{
			if (discoverClientFactory == null)
			{
				throw new ArgumentNullException(nameof(discoverClientFactory));
			}

			return discoverClientFactory
				.MakeObservable()
				.FirstOrDefaultAsync()
				.ToEnumerable()
				.FirstOrDefault();
		}
	}
}