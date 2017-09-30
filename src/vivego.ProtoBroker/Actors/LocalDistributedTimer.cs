using System.Reactive.Concurrency;
using System.Threading.Tasks;

using Proto;

namespace vivego.ProtoBroker.Actors
{
	public class LocalDistributedTimer : IDistributedTimer, IActor
	{
		private PID _actorScheduler;
		private readonly IScheduler _scheduler;
		
		public LocalDistributedTimer(IScheduler scheduler = null)
		{
			_scheduler = scheduler ?? DefaultScheduler.Instance;
		}

		public virtual Task Schedule(DeferMessage deferMessage)
		{
			if (_actorScheduler == null)
			{
				_actorScheduler = Actor.Spawn(Actor.FromProducer(() => this));
			}
			
			_actorScheduler.Tell(deferMessage);
			return Task.CompletedTask;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case DeferMessage deferMessage:
					Scheduler.Schedule<object>(_scheduler, null, deferMessage.Defer, (s, o) =>
					{
						deferMessage.Pid.Tell(deferMessage.Message);
					});
					break;
			}

			return Task.CompletedTask;
		}
	}
}