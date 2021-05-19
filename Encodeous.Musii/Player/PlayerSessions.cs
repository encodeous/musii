using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii.Player
{
    /// <summary>
    /// Manages the "playing" sessions
    /// </summary>
    public class PlayerSessions
    {
        private ProxyService _proxy;
        private IServiceProvider _provider;
        private ConcurrentDictionary<ulong, MusicPlayer> _sessions = new();
        private ConcurrentDictionary<Guid, PlayerState> _states = new();
        private ConcurrentHashSet<ulong> _loadingSessions = new();
        private ILogger<PlayerSessions> _log;
        public PlayerSessions(ProxyService proxy, IServiceProvider provider, ILogger<PlayerSessions> log)
        {
            _proxy = proxy;
            _provider = provider;
            _log = log;
        }

        public PlayerState CreateState()
        {
            var state = new PlayerState();
            return _states[state.StateId] = state;
        }
        
        public PlayerState CopyState(Guid id)
        {
            if (!_states.ContainsKey(id)) return null;
            return _states[id].CloneState();
        }

        public async Task<MusicPlayer> GetOrCreateSession(CommandContext context)
        {
            if (_sessions.ContainsKey(context.Guild.Id))
            {
                return _sessions[context.Guild.Id];
            }
            if (_loadingSessions.Contains(context.Guild.Id))
            {
                while (_loadingSessions.Contains(context.Guild.Id))
                {
                    await Task.Delay(50);
                }

                return _sessions[context.Guild.Id];
            }

            var cts = new CancellationTokenSource();
            _loadingSessions.Add(context.Guild.Id);
            var scope = _provider.CreateScope();
            var proxy = await _proxy.GetProxy(cts.Token);
            var client = scope.ServiceProvider.GetRequiredService<YoutubeService>();
            client.InitializeClient(proxy.EndPoint);
            var player = scope.ServiceProvider.GetRequiredService<MusicPlayer>();
            player.Proxy = proxy;
            player.DeletePlayer = () =>
            {
                while(!_sessions.TryRemove(context.Guild.Id, out _)){}
                cts.Cancel();
            };
            await player.Connect(context.Member.VoiceState.Channel, context.Channel);
            _log.LogDebug($"Created session for guild {context.Guild.Id} using proxy {player.Proxy.EndPoint.Address}:{player.Proxy.EndPoint.Port}");
            _sessions[context.Guild.Id] = player;
            while (!_loadingSessions.TryRemove(context.Guild.Id)) await Task.Delay(10);
            return player;
        }
    }
}