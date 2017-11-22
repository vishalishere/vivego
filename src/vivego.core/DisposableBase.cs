using System;
using System.Collections.Concurrent;
using System.Threading;

namespace vivego.core
{
	public abstract class DisposableBase : IDisposable
	{
		private readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();

		private readonly Lazy<CancellationTokenSource> _lazyCancellationTokenSource =
			new Lazy<CancellationTokenSource>(() => new CancellationTokenSource(), true);

		private long _disposeSignaled;

		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		public void Dispose()
		{
			Cleanup();

			while (_disposables.TryPop(out IDisposable disposable))
			{
				disposable.Dispose();
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