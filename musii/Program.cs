using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using YoutubeExplode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using musii.Modules;
using musii.Music;
using musii.Utilities;

namespace musii
{
    class Program
    {
        internal static IConfiguration _config;
        internal static DateTime StartTime = DateTime.Now;
        internal static DiscordSocketClient _client;
        internal static bool stop = false;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            $"Starting MUSII Bot {Config.VersionString}...".Log();
            $"Building Configuration".Log();

            _config = BuildConfig();

            $"Starting Discord Client".Log();
            _client = new DiscordSocketClient();

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            $"Connecting to Discord".Log();

            await _client.LoginAsync(TokenType.Bot, _config["token"]).ConfigureAwait(false);

            Config.SpotifyClientId = _config["spotify_id"];
            Config.SpotifyClientSecret = _config["spotify_secret"];

            await _client.StartAsync().ConfigureAwait(false);

            $"Bot started! Press Control + C to exit!".Log();

            $"Connecting to Spotify...".Log();

            SpotifyController.InitializeSpotify();

            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            while (!stop)
            {
                await _client.SetGameAsync("build " + Config.VersionString).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await _client.SetActivityAsync(new CustomActivity($" High Quality Music.",
                    ActivityType.Listening, ActivityProperties.None, "")).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await _client.SetActivityAsync(new CustomActivity($" bits at {_client.Latency} ms latency.",
                    ActivityType.Playing, ActivityProperties.None, "")).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);
            }

        }

        private void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //MusicBotModule.player.Stop();
            Console.WriteLine("Bot Stopped.");
            stop = true;
            _client.StopAsync().ConfigureAwait(false);
            Environment.Exit(0);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .BuildServiceProvider();
        }

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional:true, reloadOnChange:true)
                .Build();
        }
    }
}
