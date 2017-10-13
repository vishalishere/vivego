using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Orleans;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Streams;

using vivego.core;
using vivego.Orleans.Providers.Stream;

namespace vivego.Orleans.Providers
{
	public static class ClientConfigurationExtensions
	{
		public static ClientConfiguration AddPublishSubscribeStreamProvider(this ClientConfiguration clientConfiguration,
			string providerName = "SMSProvider",
			int inMemoryCacheSize = PublishSubscribeStreamConfiguration.DefaultInMemoryCacheSizeParam,
			int numberOfQueues = PublishSubscribeStreamConfiguration.DefaultNumOfQueues)
		{
			if (clientConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clientConfiguration));
			}

			clientConfiguration.RegisterStreamProvider<PublishSubscribeStreamProvider>(providerName,
				new Dictionary<string, string>
				{
					{
						PublishSubscribeStreamConfiguration.InMemoryCacheSizeParam,
						inMemoryCacheSize.ToString()
					},
					{
						PublishSubscribeStreamConfiguration.NumberOfQueuesParam,
						numberOfQueues.ToString()
					}
				});
			return clientConfiguration;
		}

		public static ClientConfiguration ConnectTo(this ClientConfiguration clientConfiguration,
			ClusterConfiguration clusterConfiguration)
		{
			if (clientConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clientConfiguration));
			}

			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			clientConfiguration.Gateways.Clear();
			clientConfiguration.Gateways.Add(clusterConfiguration.Defaults.ProxyGatewayEndpoint);
			return clientConfiguration;
		}

		public static IClusterClientEx Run(this ClientConfiguration clientConfiguration,
			params IOrleansStartup[] orleansStartups)
		{
			if (clientConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clientConfiguration));
			}

			ClusterClientWapper clusterClientWapper = new ClusterClientWapper();
			ClientBuilder clientBuilder = new ClientBuilder();
			clientBuilder
				.UseConfiguration(clientConfiguration)
				.ConfigureServices(collection =>
				{
					foreach (IOrleansStartup orleansStartup in orleansStartups)
					{
						orleansStartup.ConfigureServices(collection);
					}
				})
				.AddClientInvokeCallback(clusterClientWapper.Callback)
				.AddClusterConnectionLostHandler(clusterClientWapper.Handler);
			clusterClientWapper.ClusterClient = clientBuilder.Build();
			clusterClientWapper.ClusterClient.Connect().GetAwaiter().GetResult();
			return clusterClientWapper;
		}
	}

	internal class ClusterClientWapper : DisposableBase, IClusterClientEx
	{
		private readonly ISubject<(InvokeMethodRequest request, IGrain grain)> _callbackObservable =
			Subject.Synchronize(new Subject<(InvokeMethodRequest request, IGrain grain)>());

		private readonly ISubject<Unit> _connectionLostObservable =
			Subject.Synchronize(new Subject<Unit>());

		public IObservable<(InvokeMethodRequest request, IGrain grain)> CallbackObservable => _callbackObservable;
		public IObservable<Unit> ConnectionLostObservable => _connectionLostObservable;

		protected override void Cleanup()
		{
			using (ClusterClient)
			{
				if (IsInitialized)
				{
					Close().GetAwaiter().GetResult();
				}
			}
		}

		public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string grainClassNamePrefix = null)
			where TGrainInterface : IGrainWithGuidKey
		{
			return ClusterClient.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
		}

		public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string grainClassNamePrefix = null)
			where TGrainInterface : IGrainWithIntegerKey
		{
			return ClusterClient.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
		}

		public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string grainClassNamePrefix = null)
			where TGrainInterface : IGrainWithStringKey
		{
			return ClusterClient.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
		}

		public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string grainClassNamePrefix)
			where TGrainInterface : IGrainWithGuidCompoundKey
		{
			return ClusterClient.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
		}

		public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string grainClassNamePrefix)
			where TGrainInterface : IGrainWithIntegerCompoundKey
		{
			return ClusterClient.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
		}

		public Task<TGrainObserverInterface> CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj)
			where TGrainObserverInterface : IGrainObserver
		{
			return ClusterClient.CreateObjectReference<TGrainObserverInterface>(obj);
		}

		public Task DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj)
			where TGrainObserverInterface : IGrainObserver
		{
			return ClusterClient.DeleteObjectReference<TGrainObserverInterface>(obj);
		}

		public void BindGrainReference(IAddressable grain)
		{
			ClusterClient.BindGrainReference(grain);
		}

		public IEnumerable<IStreamProvider> GetStreamProviders()
		{
			return ClusterClient.GetStreamProviders();
		}

		public IStreamProvider GetStreamProvider(string name)
		{
			return ClusterClient.GetStreamProvider(name);
		}

		public Task Connect()
		{
			return ClusterClient.Connect();
		}

		public Task Close()
		{
			return ClusterClient.Close();
		}

		public void Abort()
		{
			ClusterClient.Abort();
		}

		public bool IsInitialized => ClusterClient.IsInitialized;
		public Logger Logger => ClusterClient.Logger;
		public IServiceProvider ServiceProvider => ClusterClient.ServiceProvider;
		public ClientConfiguration Configuration => ClusterClient.Configuration;
		public IClusterClient ClusterClient { get; set; }

		public void Callback(InvokeMethodRequest request, IGrain grain)
		{
			_callbackObservable.OnNext((request, grain));
		}

		public void Handler(object sender, EventArgs eventArgs)
		{
			_connectionLostObservable.OnNext(Unit.Default);
		}
	}
}