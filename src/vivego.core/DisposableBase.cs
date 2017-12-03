using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.core
{
	public abstract class DisposableBase : IDisposable
	{
		private readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();

		private readonly Lazy<CancellationTokenSource> _lazyCancellationTokenSource =
			new Lazy<CancellationTokenSource>(() => new CancellationTokenSource(), true);

		private readonly Lazy<CancellationTokenTaskSource<object>> _lazyCancellationTokenTaskSource;

		private long _disposeSignaled;

		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;
		protected Task CancellationTokenTask => _lazyCancellationTokenTaskSource.Value.Task;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		protected DisposableBase()
		{
			_lazyCancellationTokenTaskSource = new Lazy<CancellationTokenTaskSource<object>>(() => new CancellationTokenTaskSource<object>(CancellationToken), true);
		}

		public void Dispose()
		{
			Cleanup();

			while (_disposables.TryPop(out IDisposable disposable))
			{
				disposable.Dispose();
			}

			if (_lazyCancellationTokenTaskSource.IsValueCreated)
			{
				_lazyCancellationTokenTaskSource.Value.Dispose();
			}

			if (_lazyCancellationTokenSource.IsValueCreated)
			{
				using (_lazyCancellationTokenSource.Value)
				{
					_lazyCancellationTokenSource.Value.Cancel(false);
				}
			}

			// Take yourself off the finalization queue
			// to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}

		protected void RegisterDisposable(Action action)
		{
			RegisterDisposable(new AnonymousDisposable(action));
		}

		protected void RegisterDisposable(IDisposable disposable)
		{
			_disposables.Push(disposable);
		}

		/// <summary>
		///     Do cleanup here
		/// </summary>
		protected virtual void Cleanup()
		{
		}
	}
}