using System;

namespace vivego.Discovery
{
	public interface IDiscoverServer
	{
		IDisposable Run();
	}
}