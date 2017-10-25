using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using vivego.core;
using vivego.Discovery.Abstactions;

namespace vivego.Discovery.Udp
{
	public class UdpDiscoverClientFactory : DisposableBase, IDiscoverClientFactory
	{
		private readonly int _port;
		private readonly TimeSpan _queryInterval;
		private readonly TimeSpan _queryTimeout;
		private readonly UdpClient _udpClient = new UdpClient();
		private readonly byte[] _clusterNameBytes;

		public UdpDiscoverClientFactory(string clusterName,
			int port,
			TimeSpan queryTimeout,
			TimeSpan queryInterval)
		{
			if (string.IsNullOrEmpty(clusterName))
			{
				throw new ArgumentNullException(nameof(clusterName));
			}

			_port = port;
			_queryTimeout = queryTimeout;
			_queryInterval = queryInterval;

			//IPAddress multicastaddress = IPAddress.Parse("239.0.0.222");
			//_udpClient.JoinMulticastGroup(multicastaddress);
			//IPEndPoint remoteep = new IPEndPoint(multicastaddress, 2222);

			_udpClient.EnableBroadcast = true;
			_udpClient.ExclusiveAddressUse = false;
			_udpClient.Client.EnableBroadcast = true;
			_udpClient.Client.ExclusiveAddressUse = false;
			_udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
			_clusterNameBytes = Encoding.UTF8.GetBytes(clusterName);

			RegisterDisposable(_udpClient);
		}

		public IObservable<IPEndPoint[]> MakeObservable()
		{
			return Observable
				.Interval(_queryInterval)
				.SelectMany(_ => Observable.FromAsync(PollAsync));
		}

		private async Task<IPEndPoint[]> PollAsync()
		{
			Console.Out.WriteLine("Poll");
			List<UdpReceiveResult> receives = new List<UdpReceiveResult>();
			using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(_queryTimeout))
			using (CancellationTokenTaskSource<object> tokenTaskSource =
				new CancellationTokenTaskSource<object>(cancellationTokenSource.Token))
			{
				while (!cancellationTokenSource.IsCancellationRequested)
				{
					Task<UdpReceiveResult> receiveTask = _udpClient.ReceiveAsync();
					await _udpClient
						.SendAsync(_clusterNameBytes, _clusterNameBytes.Length, "255.255.255.255", _port)
						.ConfigureAwait(false);

					await Task
						.WhenAny(tokenTaskSource.Task, receiveTask)
						.ConfigureAwait(false);
					if (cancellationTokenSource.IsCancellationRequested)
					{
						break;
					}

					UdpReceiveResult recvBuffer = await receiveTask.ConfigureAwait(false);
					receives.Add(recvBuffer);
				}
			}

			return receives
				.Where(udpReceiveResult => udpReceiveResult.Buffer.SequenceEqual(_clusterNameBytes))
				.Select(udpReceiveResult => udpReceiveResult.RemoteEndPoint)
				.Distinct()
				.ToArray();
		}
	}
}