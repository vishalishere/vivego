using System;
using System.Collections.Generic;
using System.Threading;

namespace vivego.core
{
	public abstract class DisposableBase : IDisposable
	{
		private readonly List<IDisposable> _disposables = new List<IDisposable>();

		private readonly Lazy<CancellationTokenSource> _lazyCancellationTokenSource =
			new Lazy<CancellationTokenSource>(() => new CancellationTokenSource(), true);

		private long _disposeSignaled;

		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		public void Dispose()
		{
			Cleanup();
			if (_lazyCancellationTokenSource.IsValueCreated)
			{
				_lazyCancellationTokenSource.Value.Dispose();
			}

			foreach (IDisposable disposable in _disposables)
			{
				disposable.Dispose();
			}

			_disposables.Clear();

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
			_disposables.Insert(0, disposable);
		}

		/// <summary>
		///     Do cleanup here
		/// </summary>
		protected virtual void Cleanup()
		{
		}
	}
}