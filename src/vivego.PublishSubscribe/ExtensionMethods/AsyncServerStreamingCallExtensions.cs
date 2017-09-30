using System;
using System.Reactive.Linq;
using System.Threading;

using Grpc.Core;

namespace vivego.PublishSubscribe.ExtensionMethods
{
	public static class AsyncServerStreamingCallExtensions
	{
		public static IObservable<T> ToObservable<T>(this AsyncServerStreamingCall<T> serverStream)
		{
			return Observable.Create<T>(async observer =>
			{
				try
				{
					using (IAsyncStreamReader<T> responseStream = serverStream.ResponseStream)
					{
						while (await responseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
						{
							T streamCurrent = responseStream.Current;
							observer.OnNext(streamCurrent);
						}
					}

					observer.OnCompleted();
				}
				catch (OperationCanceledException)
				{
					observer.OnCompleted();
				}
				catch (RpcException rpcException) when (rpcException.Status.StatusCode == StatusCode.Cancelled)
				{
					observer.OnCompleted();
				}
				catch (Exception e)
				{
					observer.OnError(e);
				}
			});
		}
	}
}