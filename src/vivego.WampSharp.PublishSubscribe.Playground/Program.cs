using System;

using vivego.core;
using vivego.PublishSubscribe;
using vivego.Serializer.Wire;

using WampSharp.V2;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.PublishSubscribe.Playground
{
	internal class Program
	{
		public static IDisposable EnableDistributedBackplane(IWampHostedRealm realm)
		{
			IPublishSubscribe publishSubscribe = new PublishSubscribeBuilder(new WireSerializer()).Build();
			Proto.ClusterBuilder.RunSeededLocalCluster();
			return realm.EnableDistributedBackplane(publishSubscribe);
		}

		private static void Main(string[] args)
		{
			int serverPort = PortUtils.FindAvailablePortIncrementally(18889);
			string serverAddress = "ws://127.0.0.1:" + serverPort + "/ws";
			WampHost host = new DefaultWampHost(serverAddress);
			IWampHostedRealm realm = host.RealmContainer.GetRealmByName("vivego");
			EnableDistributedBackplane(realm);
			host.Open();

			DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
			IWampChannel wampChannel = channelFactory.CreateJsonChannel(serverAddress, "vivego");
			wampChannel.Open().Wait();

			using (wampChannel.RealmProxy.Services
				.GetSubject<object>("vivego")
				.Subscribe(s => { Console.Out.WriteLine(s); }))
			{
				while (true)
				{
					string input = Console.ReadLine();

					realm.TopicContainer.Publish(WampObjectFormatter.Value,
						new PublishOptions(),
						"vivego",
						new object[] { "Helo" });
				}
			}
		}
	}
}