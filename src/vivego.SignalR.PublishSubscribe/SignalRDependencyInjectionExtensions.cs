using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace vivego.SignalR.PublishSubscribe
{
	public static class SignalRDependencyInjectionExtensions
	{
		public static IServiceCollection AddSignalRPublishSubscribeHubLifetimeManager(this IServiceCollection services)
		{
			services.AddSingleton(typeof(HubLifetimeManager<>), typeof(PublishSubscribeHubLifetimeManager<>));
			return services;
		}
	}
}