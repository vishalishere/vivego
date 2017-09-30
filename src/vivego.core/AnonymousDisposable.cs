using System;

namespace vivego.core
{
	public class AnonymousDisposable : IDisposable
	{
		private readonly Action _action;

		public AnonymousDisposable(Action action)
		{
			_action = action ?? throw new ArgumentNullException(nameof(action));
		}

		public void Dispose()
		{
			_action();
		}
	}
}