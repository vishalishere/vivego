using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

using vivigo.Hosting;

namespace vivego.ConsoleTemplate
{
	public class Program
	{
		private static readonly CancellationTokenSource s_tokenSource;

		static Program()
		{
			s_tokenSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, args) =>
			{
				s_tokenSource.Cancel();
				args.Cancel = true;
			};
		}

		public static async Task Main(string[] args)
		{
			await HostingUsingOnlyInterfaceMethodsAsync(args, s_tokenSource.Token);
			//await HostingExtensionMethodsAsync(args, s_tokenSource.Token);
			//await HostingUsingStartupAsync(args, s_tokenSource.Token);
		}

		private static async Task HostingUsingOnlyInterfaceMethodsAsync(string[] args, CancellationToken ct)
		{
			ILoggerFactory loggerFactory = new LoggerFactory()
				.AddSerilog(new LoggerConfiguration()
					.MinimumLevel.Debug()
					.WriteTo.LiterateConsole(LogEventLevel.Debug)
					.CreateLogger());

			ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

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

					using (IHostRunContext<ExampleHost> ctx = hostBuilder.BuildRunContext<ExampleHost>())
					{
						await ctx.Host.RunAsync(ct);
					}
				}

				logger.LogInformation("Terminated");
			}
		}
	}
}