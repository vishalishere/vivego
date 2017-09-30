using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

using Proto;
using Proto.Persistence;

using vivego.Proto.Actor.core.Actors;
using vivego.ProtoBroker.Messages;

namespace vivego.ProtoBroker.Actors
{
	public class NotificationOptions
	{
		public Publish[] Notifications { get; set; }
		public Enqueue[] Enqueues { get; set; }
	}
	
	public class Enqueue
	{
		public Guid Id { get; set; }
		public byte[] Data { get; set; }
		public TimeSpan? Defer { get; set; }
		public TimeSpan? TimeToLive { get; set; }
		public string DebouncerKey { get; set; }
		public NotificationOptions EnqueueOptions { get; set; }
		public NotificationOptions DequeueOptions { get; set; }
		public NotificationOptions AcknowledgeOptions { get; set; }
		public NotificationOptions NotAcknowledgeOptions { get; set; }
		public NotificationOptions TimeToLiveOptions { get; set; }
		public int MaxLength { get; set; }
	}

	public class EnqueueResponse
	{
		public Guid Id { get; set; }
		public bool MaxLengthExceeded { get; set; }
		public bool Bounced { get; set; }
	}

	public class Dequeue
	{
		public TimeSpan? AcknowledgeTimeout { get; set; }
		public bool Peek { get; set; }
		public int Count { get; set; } = 1;
	}

	public class QueueEntry
	{
		public Guid Id { get; set; }
		public byte[] Data { get; set; }
	}

	public class DequeueResponse
	{
		public QueueEntry[] Entries { get; set; }
	}

	public class Setup
	{
		
	}

	public class QueueActorState
	{
		
	}

	public class QueueActorTimer : IActor
	{
		private readonly IScheduler _scheduler;

		public QueueActorTimer(IScheduler scheduler = null)
		{
			_scheduler = scheduler ?? DefaultScheduler.Instance;
		}

		public Task ReceiveAsync(IContext context)
		{
			throw new NotImplementedException();
		}
	}

	public class QueueActor : PeriodicSnapshotPersistenceActorBase<QueueActorState>
	{
		public QueueActor(string queueName,
			IProvider persistenceProvider) : base(queueName, persistenceProvider)
		{
		}

		public override async Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				default:
					await base
						.ReceiveAsync(context)
						.ConfigureAwait(false);
					break;
			}
		}
	}

	public class QueueEntryActor : IActor
	{
		private readonly Persistence _persistence;
		
		public QueueEntryActor(IProvider persistenceProvider,
			Guid id)
		{
			_persistence = Persistence.WithSnapshotting(persistenceProvider, id.ToString("N"), Apply);
		}

		private void Apply(Snapshot snapshot)
		{
			switch (snapshot)
			{
				case RecoverSnapshot msg:
					//if (msg.State is State ss)
					{
						//_state = ss;
						//Console.WriteLine("MyPersistenceActor - RecoverSnapshot = Snapshot.Index = {0}, Snapshot.State = {1}", _persistence.Index, ss.Name);
					}
					break;
			}
		}

		public async Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Started _:
					await _persistence
						.RecoverStateAsync()
						.ConfigureAwait(false);
					break;
				case Enqueue enqueue:
					context.Respond(new EnqueueResponse
					{
						Id = enqueue.Id,
						Bounced = false,
						MaxLengthExceeded = false
					});
					break;
			}
		}
	}
}