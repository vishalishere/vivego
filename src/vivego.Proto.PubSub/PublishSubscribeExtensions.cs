using System;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.Proto.PubSub
{
	public static class PublishSubscribeExtensions
	{
		//public static TResponse RequestReply<TRequest, TResponse>(this IPublishSubscribe publishSubscribe,
		//	string subject,
		//	TRequest request,
		//	TimeSpan timeout,
		//	string queue = null)
		//{
		//	using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
		//	{
		//		return publishSubscribe.RequestReplyAsync<TRequest, TResponse>(subject, request, queue, cancellationTokenSource.Token).Result;
		//	}
		//}

		//public static async Task<TResponse> RequestReplyAsync<TRequest, TResponse>(this IPublishSubscribe publishSubscribe,
		//	string subject,
		//	TRequest request,
		//	TimeSpan timeout,
		//	string queue = null)
		//{
		//	using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
		//	{
		//		return await publishSubscribe.RequestReplyAsync<TRequest, TResponse>(subject, request, queue,
		//			cancellationTokenSource.Token);
		//	}
		//}

		//public static async Task<TResponse> RequestReplyAsync<TRequest, TResponse>(this IPublishSubscribe publishSubscribe,
		//	string subject,
		//	TRequest request,
		//	string queue = null,
		//	CancellationToken? cancellationToken = null)
		//{
		//	string replyTo = Guid.NewGuid().ToString();
		//	TaskCompletionSource<TResponse> taskCompletionSource = new TaskCompletionSource<TResponse>();
		//	using (publishSubscribe
		//		.Observe<TResponse>(replyTo, queue)
		//		.Subscribe(tuple => taskCompletionSource.TrySetResult(tuple.Data),
		//			exception => taskCompletionSource.TrySetException(exception),
		//			() => taskCompletionSource.TrySetCanceled()))
		//	{
		//		publishSubscribe.Publish(subject, new Message<TRequest>(replyTo, request));

		//		CancellationToken actualCancellationToken = cancellationToken.GetValueOrDefault();
		//		Task cancellationTokenTask = actualCancellationToken.AsTask();
		//		await Task.WhenAny(cancellationTokenTask, taskCompletionSource.Task);
		//		actualCancellationToken.ThrowIfCancellationRequested();
		//		return await taskCompletionSource.Task;
		//	}
		//}
	}
}