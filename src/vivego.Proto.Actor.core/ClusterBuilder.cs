using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;

using vivego.core;
using vivego.Proto.ClusterProvider;

namespace vivego.Proto
{
	public class Cluster
	{
		public static IDisposable RunSeededLocalCluster(string clusterId = null,
			int port = 35100)
		{
			IPAddress ipAddress = NetworkUtils.GetLocalIpAddress().First();
			int serverPort = PortUtils.FindAvailablePortIncrementally(port);
			IPEndPoint[] seedsEndpoints = {new IPEndPoint(ipAddress, serverPort)};
			global::Proto.Cluster.Cluster.Start(clusterId, ipAddress.ToString(), serverPort,
				new SeededLocalClusterProvider(Observable.Return(seedsEndpoints)));
			return new AnonymousDisposable(() => global::Proto.Cluster.Cluster.Shutdown());
		}
	}
}