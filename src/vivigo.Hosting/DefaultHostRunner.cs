using System;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace vivigo.Hosting
{
	public class DefaultHostRunner
	{
		public static void Run<THost>(params string[] args) where THost : class, IHost
		{
			using (CancellationTokenSource tokenSource = new CancellationTokenSource())
			{
				void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs cancelEventArgs)
				{
					// ReSharper disable once AccessToDisposedClosure
					tokenSource.Cancel();
					cancelEventArgs.Cancel = true;
				}
				Console.CancelKeyPress += CancelKeyPressHandler;
				try
				{
					ILoggerFactory loggerFactory = new LoggerFactory()
						.AddSerilog(new LoggerConfiguration()
							.MinimumLevel.Debug()
							.WriteTo.LiterateConsole(LogEventLevel.Debug)
							.CreateLogger());

					ILogger<DefaultHostRunner> logger = loggerFactory.CreateLogger<DefaultHostRunner>();
					using (logger.BeginScope("Running"))
					{
						using (HostBuilder hostBuilder = new HostBuilder("ASPNETCORE_ENVIRONMENT"))
						{
							hostBuilder.SetLoggerFactory(loggerFactory);
							hostBuilder.AddConfigurationBuilderHandler(param =>
							{
								param.Builder
									.SetBasePath(param.Environment.ContentRootPath)
									.AddJsonFile("appsettings.json", true, true)
									.AddJsonFile($"appsettings.{param.Environment.Name}.json", true, true)
									.AddEnvironmentVariables()
									.AddCommandLine(args);
							});
							hostBuilder.AddConfigurationHandler(param => { });
							hostBuilder.AddLoggerFactoryHandler(param => { });
							hostBuilder.AddServiceCollectionHandler(param => { });
							//hostBuilder.ServiceProviderBuilder = param =>
							//{
							//};

							using (IHostRunContext<THost> ctx = hostBuilder.BuildRunContext<THost>())
							{
								ctx.Host.RunAsync(tokenSource.Token).GetAwaiter().GetResult();
							}
						}

						logger.LogInformation("Terminated");
					}
				}
				finally
				{
					Console.CancelKeyPress -= CancelKeyPressHandler;
				}
			}
		}
	}
}