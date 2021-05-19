using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using Encodeous.DirtyProxy;
using Encodeous.Musii.Network;
using Encodeous.Musii.Player;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;
using YoutubeExplode;

namespace Encodeous.Musii
{
    public class Musii
    {
        public async Task Run()
        {
            if (!File.Exists("appsettings.json"))
            {
                var loader = new ResourceLoader();
                await File.WriteAllTextAsync("appsettings.json",loader.GetEmbeddedResourceString(this.GetType().Assembly, "appsettings.json"));
            }
            var appHost = Host.CreateDefaultBuilder()
                .UseSystemd()
                .ConfigureServices(async (context, services) =>
                {
                    // add services
                    services.AddSingleton<ProxyService>();
                    services.AddSingleton<PlayerSessions>();
                    services.AddSingleton<SpotifyService>();
                    // add discord
                    AddDiscord(context, services);
                    // per-play session services
                    services.AddScoped<FFMpegService>();
                    services.AddScoped<YoutubeService>();
                    services.AddScoped<SearchService>();
                    services.AddScoped<MusicPlayer>();
                    // add application
                    services.AddHostedService<HostedBot>();
                }).Build();
            // warm up proxies
            appHost.Services.GetService<ProxyService>();
            // setup application
            Setup(appHost.Services);
            await appHost.RunAsync();
        }

        private void AddDiscord(HostBuilderContext context, IServiceCollection services)
        {
            // add discord client
            services.AddSingleton(x =>
            {
                var logFactory = x.GetService<ILoggerFactory>();
                var discord = new DiscordClient(new()
                {
                    Token = context.Configuration["musii:DiscordToken"],
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,
                    LoggerFactory = logFactory,
                });
                // add discord services that we dont need to store separately
                discord.UseVoiceNext();
                discord.UseInteractivity(new InteractivityConfiguration() 
                { 
                    PollBehaviour = PollBehaviour.DeleteEmojis,
                    Timeout = TimeSpan.FromMinutes(1)
                });
                return discord;
            });
        }
        private void Setup(IServiceProvider collection)
        {
            var log = collection.GetRequiredService<ILogger<Musii>>();
            var client = collection.GetRequiredService<DiscordClient>();
            var config = collection.GetRequiredService<IConfiguration>();

            // add services to discord
            
            var commands = client.UseCommandsNext(new ()
            { 
                StringPrefixes = new[] { config["musii:DefaultPrefix"] },
                Services = collection
            });
            
            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.CommandErrored += (sender, args) =>
            {
                log.LogError($"Exception occurred while executing command: {args.Exception}");
                return Task.CompletedTask;
            };

            if (Directory.Exists("/etc/systemd/system/") && !SystemdHelpers.IsSystemdService())
            {
                log.LogWarning("Musii has detected that your system has systemd. " +
                               "It is highly recommended to run Musii under systemd. " +
                               "See: https://devblogs.microsoft.com/dotnet/net-core-and-systemd/ for setup instructions");
            }
        }
    }
}