using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii
{
    public class HostedBot : IHostedService
    {
        private DiscordClient _client;
        private ILogger<HostedBot> _log;

        public HostedBot(DiscordClient client, ILogger<HostedBot> log)
        {
            _client = client;
            _log = log;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting discord client");
            await _client.ConnectAsync();
            
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.DisconnectAsync();
        }
    }
}