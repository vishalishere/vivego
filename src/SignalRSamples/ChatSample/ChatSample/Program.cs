using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using vivego.core;

namespace SignalRChatSample
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args)
		{
			return WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.UseUrls("http://localhost:" + PortUtils.FindAvailablePortIncrementally(5050))
				.Build();
		}
	}
}