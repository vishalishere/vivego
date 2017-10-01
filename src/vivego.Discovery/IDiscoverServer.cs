using System;

namespace vivego.Discovery.Abstactions
{
	public interface IDiscoverServer
	{
		IDisposable Run();
	}
}