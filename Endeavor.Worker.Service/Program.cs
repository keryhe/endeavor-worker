using Endeavor.Worker.Messaging;
using Endeavor.Worker.Persistence;
using Keryhe.Messaging.RabbitMQ.Extensions;
using Keryhe.Persistence.SqlServer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Endeavor.Worker.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services.AddSqlServerProvider(hostContext.Configuration.GetSection("SqlServerOptions"));
                    services.AddRabbitMQListener<TaskToBeWorked>(hostContext.Configuration.GetSection("RabbitMQListener"));

                    services.AddSingleton<IDal, WorkerDal>();
                    services.AddSingleton<IHostedService, Worker>();

                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            await builder.RunConsoleAsync();
        }
    }
}
