using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace vivego.Discovery.DotNetty
{
	internal static class NetUtil
	{
		public static IPAddress GetLoopbackAddress(AddressFamily addressFamily)
		{
			switch (addressFamily)
			{
				case AddressFamily.InterNetwork:
					return IPAddress.Loopback;
				case AddressFamily.InterNetworkV6:
					return IPAddress.IPv6Loopback;
			}

			throw new NotSupportedException(
				$"Address family {addressFamily} is not supported. Expecting InterNetwork/InterNetworkV6");
		}

		public static NetworkInterface LoopbackInterface(AddressFamily addressFamily)
		{
			NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			switch (addressFamily)
			{
				case AddressFamily.InterNetwork:
					return networkInterfaces[NetworkInterface.LoopbackInterfaceIndex];
				case AddressFamily.InterNetworkV6:
					return networkInterfaces[NetworkInterface.IPv6LoopbackInterfaceIndex];
			}

			throw new NotSupportedException(
				$"Address family {addressFamily} is not supported. Expecting InterNetwork/InterNetworkV6");
		}
	}
}