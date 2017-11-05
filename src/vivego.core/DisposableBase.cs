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

		private readonly Lazy<CancellationTokenTaskSource<object>> _cancellationTokenTaskSource;

		private long _disposeSignaled;

		protected Task CancellationTask => _cancellationTokenTaskSource.Value.Task;
		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		protected DisposableBase()
		{
			_cancellationTokenTaskSource = new Lazy<CancellationTokenTaskSource<object>>(() => new CancellationTokenTaskSource<object>(CancellationToken), true);
		}

		public void Dispose()
		{
			Cleanup();
			if (_cancellationTokenTaskSource.IsValueCreated)
			{
				_cancellationTokenTaskSource.Value.Dispose();
			}

			if (_lazyCancellationTokenSource.IsValueCreated)
			{
				_lazyCancellationTokenSource.Value.Dispose();
			}

			while (_disposables.TryPop(out IDisposable disposable))
			{
				disposable.Dispose();
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