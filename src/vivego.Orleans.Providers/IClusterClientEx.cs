using System;
using System.Reactive;

using Orleans;
using Orleans.CodeGeneration;

namespace vivego.Orleans.Providers
{
	public interface IClusterClientEx : IClusterClient
	{
		IClusterClient ClusterClient { get; }
		IObservable<(InvokeMethodRequest request, IGrain grain)> CallbackObservable { get; }
		IObservable<Unit> ConnectionLostObservable { get; }
	}
}