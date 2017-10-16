using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using vivego.Proto.PubSub;

namespace vivego.SignalR.Proto.PubSub
{
	public class ProtoPubSubHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
	{
		private readonly IPublishSubscribe _publishSubscribe;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly DefaultHubLifetimeManager<THub> _defaultHubLifetimeManager = new DefaultHubLifetimeManager<THub>();

		private readonly string _addGroupAsyncTopic = MakeTopic(nameof(AddGroupAsync));
		private readonly string _invokeAllAsyncTopic = MakeTopic(nameof(InvokeAllAsync));
		private readonly string _invokeAllExceptAsyncTopic = MakeTopic(nameof(InvokeAllExceptAsync));
		private readonly string _invokeConnectionAsyncTopic = MakeTopic(nameof(InvokeConnectionAsync));
		private readonly string _invokeGroupAsyncTopic = MakeTopic(nameof(InvokeGroupAsync));
		private readonly string _invokeUserAsyncTopic = MakeTopic(nameof(InvokeUserAsync));
		private readonly string _removeGroupAsyncTopic = MakeTopic(nameof(RemoveGroupAsync));

		public ProtoPubSubHubLifetimeManager(IPublishSubscribe publishSubscribe)
		{
			_publishSubscribe = publishSubscribe ?? throw new ArgumentNullException(nameof(publishSubscribe));
			Setup();
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel(false);
		}

		public override Task OnConnectedAsync(HubConnectionContext connection)
		{
			return _defaultHubLifetimeManager.OnConnectedAsync(connection);
		}

		public override Task OnDisconnectedAsync(HubConnectionContext connection)
		{
			return _defaultHubLifetimeManager.OnDisconnectedAsync(connection);
		}

		private static string MakeTopic(string group)
		{
			return $"{typeof(ProtoPubSubHubLifetimeManager<THub>).FullName}_{group}";
		}

		public override Task InvokeAllAsync(string methodName, object[] args)
		{
			_publishSubscribe.Publish(_invokeAllAsyncTopic, (methodName, args));
			return Task.CompletedTask;
		}

		public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
		{
			_publishSubscribe.Publish(_invokeAllExceptAsyncTopic, (methodName, args, excludedIds));
			return Task.CompletedTask;
		}

		public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
		{
			_publishSubscribe.Publish(_invokeConnectionAsyncTopic, (methodName, methodName, args));
			return Task.CompletedTask;
		}

		public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
		{
			_publishSubscribe.Publish(_invokeGroupAsyncTopic, (methodName, methodName, args));
			return Task.CompletedTask;
		}

		public override Task InvokeUserAsync(string userId, string methodName, object[] args)
		{
			_publishSubscribe.Publish(_invokeUserAsyncTopic, (methodName, methodName, args));
			return Task.CompletedTask;
		}

		public override Task AddGroupAsync(string connectionId, string groupName)
		{
			_publishSubscribe.Publish(_addGroupAsyncTopic, (connectionId, groupName));
			return Task.CompletedTask;
		}

		public override Task RemoveGroupAsync(string connectionId, string groupName)
		{
			_publishSubscribe.Publish(_removeGroupAsyncTopic, (connectionId, groupName));
			return Task.CompletedTask;
		}

		private void Setup()
		{
			_publishSubscribe
				.Observe<(string connectionId, string groupName)>(_addGroupAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.AddGroupAsync(tuple.Data.connectionId, tuple.Data.connectionId).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string methodName, object[] args)>(_invokeAllAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.InvokeAllAsync(tuple.Data.methodName, tuple.Data.args).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string methodName, object[] args, IReadOnlyList<string> excludedIds)>(_invokeAllExceptAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.InvokeAllExceptAsync(tuple.Data.methodName, tuple.Data.args, tuple.Data.excludedIds).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string connectionId, string methodName, object[] args)>(_invokeConnectionAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.InvokeConnectionAsync(tuple.Data.connectionId, tuple.Data.methodName, tuple.Data.args).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string groupName, string methodName, object[] args)>(_invokeGroupAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.InvokeGroupAsync(tuple.Data.groupName, tuple.Data.methodName, tuple.Data.args).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string userId, string methodName, object[] args)>(_invokeUserAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.InvokeUserAsync(tuple.Data.userId, tuple.Data.methodName, tuple.Data.args).GetAwaiter().GetResult(), _cancellationTokenSource.Token);

			_publishSubscribe
				.Observe<(string connectionId, string groupName)>(_removeGroupAsyncTopic)
				.Subscribe(tuple => _defaultHubLifetimeManager.RemoveGroupAsync(tuple.Data.connectionId, tuple.Data.groupName).GetAwaiter().GetResult(), _cancellationTokenSource.Token);
		}
	}
}