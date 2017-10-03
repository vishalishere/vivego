using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using vivego.core;

using WampSharp.V2;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.Proto.SubPub.Backplane.Playground
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			int serverPort = PortUtils.FindAvailablePortIncrementally(18889);
			string serverAddress = "ws://127.0.0.1:" + serverPort + "/ws";
			WampHost host = new DefaultWampHost(serverAddress);
			IWampHostedRealm realm = host.RealmContainer.GetRealmByName("vivego");
			//realm.EnableDistributedBackplane();
			host.Open();

			realm.Services.GetSubject("wamp.subscription.on_subscribe")
				.Subscribe(serialized =>
				{
					Console.Out.WriteLine("New Subscription");
				});

			DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
			IWampChannel wampChannel = channelFactory.CreateJsonChannel(serverAddress, "vivego");
			wampChannel.Open().Wait();
			ISubject<object> subject = wampChannel.RealmProxy.Services.GetSubject<object>("vivego.accid.case.caseid");

			subject.Subscribe(s => { Console.Out.WriteLine(s); });
			while (true)
			{
				var input = Console.ReadLine();
				realm.TopicContainer.Publish(WampObjectFormatter.Value,
					new PublishOptions
					{
						
					},
					"vivego",
					new object[] { "Helo" });
			}
		}
	}
}