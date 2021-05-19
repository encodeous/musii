using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
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
        private IServiceProvider _provider;
        private ConcurrentDictionary<ulong, MusicPlayer> _sessions = new();
        private ConcurrentDictionary<Guid, PlayerState> _states = new();
        private ConcurrentHashSet<ulong> _loadingSessions = new();
        private ILogger<PlayerSessions> _log;
        public PlayerSessions(IServiceProvider provider, ILogger<PlayerSessions> log)
        {
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

        public async Task<MusicPlayer> GetOrCreateSession(DiscordGuild guild, DiscordChannel voiceChannel, DiscordChannel textChannel)
        {
            if (_sessions.ContainsKey(guild.Id))
            {
                return _sessions[guild.Id];
            }
            if (_loadingSessions.Contains(guild.Id))
            {
                while (_loadingSessions.Contains(guild.Id))
                {
                    await Task.Delay(50);
                }

                return _sessions[guild.Id];
            }
            _loadingSessions.Add(guild.Id);
            var scope = _provider.CreateScope();
            // setup playback data
            var data = scope.ServiceProvider.GetRequiredService<ScopeData>();
            data.TextChannel = textChannel;
            data.VoiceChannel = voiceChannel;
            data.DeletePlayerCallback = async () =>
            {
                while (!_sessions.TryRemove(guild.Id, out _) && _sessions.ContainsKey(guild.Id))
                {
                    await Task.Delay(100);
                }
            };
            
            var player = scope.ServiceProvider.GetRequiredService<MusicPlayer>();
            await player.ConnectPlayer();
            player.InitializeState(CreateState());
            _log.LogDebug($"Created session for guild {guild.Id}");
            _sessions[guild.Id] = player;
            while (!_loadingSessions.TryRemove(guild.Id)) await Task.Delay(10);
            return player;
        }
    }
}