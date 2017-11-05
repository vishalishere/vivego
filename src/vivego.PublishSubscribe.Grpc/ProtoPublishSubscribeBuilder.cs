using System;
using System.Collections.Generic;
using System.Net;

namespace vivego.PublishSubscribe.Grpc
{
	public static class ProtoPublishSubscribeBuilder
	{
		public static PublishSubscribeBuilder GrpcPublishSubscribe(this PublishSubscribeBuilder publishSubscribeBuilder,
			IPEndPoint serverEndPoint,
			IObservable<IEnumerable<DnsEndPoint>> seedsEndpointObservable)
		{
			return publishSubscribeBuilder.Factory(builder =>
				new PublishSubscribe(serverEndPoint, seedsEndpointObservable, builder.LoggerFactory, builder.Serializer));
		}
	}
}