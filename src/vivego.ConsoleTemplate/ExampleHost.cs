using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using vivigo.Hosting;

namespace vivego.ConsoleTemplate
{
    public class ExampleHost : IHost
    {
        private readonly IHostingEnvironment _env;
        private readonly ILogger<ExampleHost> _logger;

        public ExampleHost(IHostingEnvironment env, ILogger<ExampleHost> logger)
        {
            _env = env;
            _logger = logger;
        }

        public Task RunAsync(CancellationToken ct)
        {
            _logger.LogInformation("EnvironmentName:'{environmentName}' ContentRootPath:'{contentRootPath}'", _env.Name, _env.ContentRootPath);
            return Task.CompletedTask;
        }
    }
}