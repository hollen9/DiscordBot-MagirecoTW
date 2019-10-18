using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MitamaBot.Services;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using MitamaBot.Services.Backgrounds;

namespace MitamaBot
{
    class Program
    {
        // There is no need to implement IDisposable like before as we are
        // using dependency injection, which handles calling Dispose for us.
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private IConfiguration _config;

        public async Task MainAsync()
        {
            var configBasePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(configBasePath)
#if DEBUG
                .AddJsonFile("credentials.DEBUG.json")
#else
                .AddJsonFile("credentials.RELEASE.json")
#endif
                .AddJsonFile("appsettings.json");

            _config = builder.Build();

            var TOKEN = _config.GetSection("Discord-Token").Value;
            Console.WriteLine(
                $"Token: {TOKEN}\n" +
                $"Owner Id: {_config.GetSection("OwnerInfo:Id").Value}\n" +
                $"Owner Name: {_config.GetSection("OwnerInfo:Name").Value}\n"
                );

            var prefixes = _config.GetSection("Prefix:StringAliases").GetChildren().ToArray().Select(c => c.Value).ToArray();
            
            Console.WriteLine(
                $"Prefixes: {string.Join('|', prefixes)}"
                );
            
            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, TOKEN);
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton<LoggingService>()

                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<ResponsiveService>()
                .AddSingleton<Services.DataStore.MagirecoInfoService>()
                .AddSingleton<NetaSoundService>()
                .BuildServiceProvider();
        }
    }
}
