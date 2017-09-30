using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Google.Protobuf;

using Proto;

using vivego.ProtoBroker.Messages;

namespace vivego.ProtoBroker.Actors
{
	/// <inheritdoc />
	/// <summary>
	/// One instance per cluster node
	/// </summary>
	public class SubscriptionActor : IActor
	{
		private List<PID> _publishers = new List<PID>();

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case RegisterPublisher registerPublisher:
					return Task.CompletedTask;
				case UnRegisterPublisher unRegisterPublisher:
					return Task.CompletedTask;
				default:
					return Task.CompletedTask;
			}
		}
	}

	public class RegisterPublisher
	{
		public PID PublisherPid { get; }

		public RegisterPublisher(PID publisherPid)
		{
			PublisherPid = publisherPid;
		}
	}

	public class UnRegisterPublisher
	{
		public PID PublisherPid { get; }

		public UnRegisterPublisher(PID publisherPid)
		{
			PublisherPid = publisherPid;
		}
	}

	public class PublisherActor : IActor
	{
		private readonly SubscriptionInfoDb<PID> _subscriptions = new SubscriptionInfoDb<PID>();

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Publish publish:
					foreach ((PID, object) tuple in _subscriptions.Select(publish.Topic, publish.Message))
					{
						tuple.Item1.Tell(publish);
					}

					return Task.CompletedTask;
				case Subscribe subscribe:
					SubscriptionInfo subscriptionInfo = new SubscriptionInfo(Type.GetType(subscribe.Type), subscribe.Topic);
					_subscriptions.Add(subscriptionInfo, subscribe.ConsumerActor);
					SubscribeAck subscribeAck = new SubscribeAck
					{
						SubscriptionId = ByteString.CopyFrom(subscriptionInfo.SubscriptionId.ToByteArray())
					};
					context.Respond(subscribeAck);
					return Task.CompletedTask;
				case Unsubscribe unsubscribe:
					UnsubscribeAck unsubscribeAck = new UnsubscribeAck
					{
						Success = _subscriptions.Remove(new Guid(unsubscribe.SubscriptionId.ToByteArray()))
					};
					context.Respond(unsubscribeAck);
					return Task.CompletedTask;
				default:
					return Task.CompletedTask;
			}
		}
	}
}