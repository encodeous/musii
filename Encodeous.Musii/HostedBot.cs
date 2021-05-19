﻿using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii
{
    public class HostedBot : IHostedService
    {
        private DiscordClient _client;
        private IConfiguration _config;
        private ILogger<HostedBot> _log;

        public HostedBot(DiscordClient client, ILogger<HostedBot> log, IConfiguration config)
        {
            _client = client;
            _log = log;
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting discord client");
            await _client.ConnectAsync();
            var endpoint = new ConnectionEndpoint
            {
                Hostname = _config["musii:LavalinkHost"],
                Port = int.Parse(_config["musii:LavalinkPort"])
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = _config["musii:LavalinkPassword"],
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            await _client.GetLavalink().ConnectAsync(lavalinkConfig);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.DisconnectAsync();
        }
    }
}