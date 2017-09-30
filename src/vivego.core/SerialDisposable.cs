using System;

namespace vivego.core
{
	/// <inheritdoc />
	/// <summary>
	/// From system.Reactive
	/// </summary>
	public class SerialDisposable : DisposableBase
	{
		private readonly object _gate = new object();
		private IDisposable _current;

		/// <summary>Gets or sets the underlying disposable.</summary>
		/// <remarks>
		///     If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of
		///     the given disposable object. Assigning this property disposes the previous disposable object.
		/// </remarks>
		public IDisposable Disposable
		{
			get
			{
				lock (_gate)
				{
					return _current;
				}
			}
			set
			{
				IDisposable disposable = null;
				lock (_gate)
				{
					if (!IsDisposed)
					{
						disposable = _current;
						_current = value;
					}
				}

				disposable?.Dispose();
				if (!IsDisposed || value == null)
				{
					return;
				}

				value.Dispose();
			}
		}

		protected override void Cleanup()
		{
			lock (_gate)
			{
				_current?.Dispose();
				_current = null;
			}
		}
	}
}