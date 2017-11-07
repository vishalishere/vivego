using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;

namespace vivego.PublishSubscribe.Grpc
{
	public static class ProtoPublishSubscribeBuilder
	{
		public static PublishSubscribeBuilder GrpcPublishSubscribe(this PublishSubscribeBuilder publishSubscribeBuilder,
			IPEndPoint serverEndPoint = null,
			IObservable<IEnumerable<DnsEndPoint>> seedsEndpointObservable = null)
		{
			serverEndPoint = serverEndPoint ?? new IPEndPoint(IPAddress.Loopback, 35000);
			seedsEndpointObservable = seedsEndpointObservable ?? Observable.Return(new[] {new DnsEndPoint("localhost", 35000)});
			return publishSubscribeBuilder.Factory(builder =>
				new PublishSubscribe(serverEndPoint, seedsEndpointObservable, builder.LoggerFactory, builder.Serializer));
		}
	}
}