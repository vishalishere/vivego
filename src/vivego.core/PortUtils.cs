using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace vivego.core
{
	public static class PortUtils
	{
		public static int FindAvailablePortIncrementally(int port)
		{
			int portAvailable = Enumerable
				.Range(port, 1000)
				.FirstOrDefault(actualPort => new IPEndPoint(IPAddress.Loopback, actualPort).IsAvailable());
			return portAvailable;
		}

		public static bool IsAvailable(this IPEndPoint endPoint)
		{
			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list, and if our port we would like to use
			// in our TcpClient is occupied, we will set isAvailable to false.
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
			foreach (IPEndPoint tcpi in tcpConnInfoArray)
			{
				if (tcpi.Port == endPoint.Port)
				{
					return false;
				}
			}

			return true;
		}

		public static int FindAvailablePort()
		{
			using (Mutex mutex = new Mutex(false, "PortUtils.FindAvailablePort"))
			{
				try
				{
					mutex.WaitOne();
					IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 0);
					using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
					{
						socket.Bind(endPoint);
						IPEndPoint local = (IPEndPoint) socket.LocalEndPoint;
						return local.Port;
					}
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			}
		}
	}
}