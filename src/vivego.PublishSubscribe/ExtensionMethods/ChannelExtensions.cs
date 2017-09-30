using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;

namespace vivego.PublishSubscribe
{
	public static class ChannelExtensions
	{
		public static Task WaitForConnection(this Channel channel)
		{
			return WaitForConnection(channel, CancellationToken.None);
		}

		public static async Task WaitForConnection(this Channel channel, CancellationToken cancellationToken)
		{
			Task cancellationTask = Task.FromCanceled<object>(cancellationToken);
			while (true)
			{
				switch (channel.State)
				{
					case ChannelState.TransientFailure:
					case ChannelState.Connecting:
						Task waitForStateChangedTask = channel.WaitForStateChangedAsync(channel.State);
						await Task.WhenAny(cancellationTask, waitForStateChangedTask).ConfigureAwait(true);
						break;
					case ChannelState.Idle:
						await channel.ConnectAsync().ConfigureAwait(false);
						break;
					case ChannelState.Ready:
					case ChannelState.Shutdown:
						return;
				}
			}
		}
	}
}