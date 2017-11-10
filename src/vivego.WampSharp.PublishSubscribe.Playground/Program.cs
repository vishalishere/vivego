using System;
using System.Collections.Generic;
using System.Linq;
using vivego.core;
using vivego.PublishSubscribe;
using vivego.Serializer.Wire;

using WampSharp.V2;
using WampSharp.V2.Authentication;
using WampSharp.V2.Client;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.PublishSubscribe.Playground
{
	public class VergicWampAuthenticatorFactory : IWampSessionAuthenticatorFactory
	{
		private readonly IDictionary<string, string> _mUserToTicket =
			new Dictionary<string, string>
			{
				["peter"] = "magic_secret_1",
				["joe"] = "magic_secret_2"
			};

		public IWampSessionAuthenticator GetSessionAuthenticator(WampPendingClientDetails details, IWampSessionAuthenticator transportAuthenticator)
		{
			HelloDetails helloDetails = details.HelloDetails;
			if (helloDetails.AuthenticationMethods?.Contains("ticket") != true)
			{
				throw new WampAuthenticationException("supports only 'ticket' authentication");
			}

			string user = helloDetails.AuthenticationId;
			if (user == null ||
				!_mUserToTicket.TryGetValue(user, out string ticket))
			{
				throw new WampAuthenticationException($"no user with authid '{user}' in user database");
			}

			return new TicketSessionAuthenticator(user, ticket, new BackendStaticAuthorizer());
		}
	}

	public class TicketSessionAuthenticator : WampSessionAuthenticator
	{
		private readonly string _mTicket;
		private readonly IWampAuthorizer _authorizer;

		public TicketSessionAuthenticator(string authenticationId, string ticket, IWampAuthorizer authorizer)
		{
			AuthenticationId = authenticationId;
			_mTicket = ticket;
			_authorizer = authorizer;
		}

		public override void Authenticate(string signature, AuthenticateExtraData extra)
		{
			if (signature == _mTicket)
			{
				IsAuthenticated = true;
				Authorizer = _authorizer;
				WelcomeDetails = new WelcomeDetails
				{
					AuthenticationProvider = "dynamic",
					AuthenticationRole = "user"
				};
			}
		}

		public override string AuthenticationId { get; }

		public override string AuthenticationMethod => "ticket";
	}

	public class BackendStaticAuthorizer : IWampAuthorizer
	{
		public BackendStaticAuthorizer(string[] authentications)
		{
		}

		public bool CanRegister(RegisterOptions options, string procedure) => true;

		public bool CanCall(CallOptions options, string procedure) => true;

		public bool CanPublish(PublishOptions options, string topicUri) => true;

		public bool CanSubscribe(SubscribeOptions options, string topicUri)
		{
			return topicUri != "com.example.topic2";
		}
	}

	public class TicketAuthenticator : IWampClientAuthenticator
	{
		private static readonly string[] _authenticationMethods = { "ticket" };

		private readonly IDictionary<string, string> _tickets =
			new Dictionary<string, string>
			{
				{"peter", "magic_secret_1"},
				{"joe", "magic_secret_2"}
			};

		private const string User = "peter";

		public AuthenticationResponse Authenticate(string authmethod, ChallengeDetails extra)
		{
			if (authmethod == "ticket")
			{
				Console.WriteLine("authenticating via '" + authmethod + "'");
				AuthenticationResponse result = new AuthenticationResponse { Signature = _tickets[User] };
				return result;
			}

			throw new WampAuthenticationException("don't know how to authenticate using '" + authmethod + "'");
		}

		public string[] AuthenticationMethods => _authenticationMethods;
		public string AuthenticationId => User;
	}

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
			string serverAddress = $"ws://127.0.0.1:{serverPort}/ws";

			//WampHost host = new DefaultWampHost(serverAddress);
			DefaultWampAuthenticationHost host = new DefaultWampAuthenticationHost(serverAddress, new VergicWampAuthenticatorFactory());

			IWampHostedRealm realm = host.RealmContainer.GetRealmByName("vivego");
			//EnableDistributedBackplane(realm);
			host.Open();

			DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
			IWampChannel wampChannel = channelFactory.CreateJsonChannel(serverAddress, "vivego", new TicketAuthenticator());
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