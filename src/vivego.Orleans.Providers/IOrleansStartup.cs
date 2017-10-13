using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace vivego.Orleans.Providers
{
	public interface IOrleansStartup
	{
		IServiceProvider ConfigureServices(IServiceCollection services);
	}

	public class OrleansStartup
	{
		private static readonly List<Type> s_orleansStartupServices = new List<Type>();
		private static readonly List<IOrleansStartup> s_orleansStartupInstanceServices = new List<IOrleansStartup>();

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			foreach (IOrleansStartup orleansStartup in s_orleansStartupInstanceServices)
			{
				orleansStartup.ConfigureServices(services);
			}

			foreach (Type type in s_orleansStartupServices)
			{
				IOrleansStartup orleansStartup = (IOrleansStartup) Activator.CreateInstance(type);
				orleansStartup.ConfigureServices(services);
			}

			IServiceProvider serviceProvider = new DefaultServiceProviderFactory()
				.CreateServiceProvider(services);
			return serviceProvider;
		}

		public static void Register<T>() where T : IOrleansStartup, new()
		{
			s_orleansStartupServices.Add(typeof(T));
		}

		public static void Register(IOrleansStartup orleansStartup)
		{
			s_orleansStartupInstanceServices.Add(orleansStartup);
		}
	}
}