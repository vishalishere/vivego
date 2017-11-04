namespace vivego.PublishSubscribe.ProtoActor
{
	public static class ProtoPublishSubscribeBuilder
	{
		public static PublishSubscribeBuilder ProtoActorPublishSubscribe(this PublishSubscribeBuilder publishSubscribeBuilder)
		{
			return publishSubscribeBuilder.Factory(builder =>
				new PublishSubscribe(builder.ClusterName, builder.Serializer, builder.LoggerFactory));
		}
	}
}