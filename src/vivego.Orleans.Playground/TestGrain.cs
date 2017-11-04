using System.Threading.Tasks;

using Orleans;
using Orleans.Streams;

using vivego.PublishSubscribe;

namespace vivego.Orleans.Playground
{
	public interface ITestGrain : IGrainWithGuidKey
	{
		Task Run();
	}

	[ImplicitStreamSubscription("SendToSelf")]
	public class TestGrain : Grain, ITestGrain
	{
		private readonly IPublishSubscribe _publishSubscribe;
		private StreamSubscriptionHandle<string> _subscription;

		public TestGrain(IPublishSubscribe publishSubscribe)
		{
			_publishSubscribe = publishSubscribe;
		}

		public Task Run()
		{
			return GetStreamProvider("SMSProvider")
				.GetStream<string>(this.GetPrimaryKey(), "SendToSelf")
				.OnNextAsync("Hello World!");
		}

		public override async Task OnActivateAsync()
		{
			_subscription = await GetStreamProvider("SMSProvider")
				.GetStream<string>(this.GetPrimaryKey(), "SendToSelf")
				.SubscribeAsync(OnNextAsync);
			await base.OnActivateAsync();
		}

		private Task OnNextAsync(string s, StreamSequenceToken streamSequenceToken)
		{
			_publishSubscribe.Publish("OnNextAsync", s);
			return Task.CompletedTask;
		}

		public override async Task OnDeactivateAsync()
		{
			await _subscription.UnsubscribeAsync();
			await base.OnDeactivateAsync();
		}
	}
}