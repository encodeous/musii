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
        private ConcurrentDictionary<ulong, GuildPlayerManager> _newSessions = new();
        private ConcurrentDictionary<Guid, PlayerRecord> _records = new();

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
            var state = new PlayerState(new ());
            return _states[state.StateId] = state;
        }
        
        public PlayerState GetState(Guid id)
        {
            if (!_states.ContainsKey(id)) return null;
            return _states[id];
        }
        
        public PlayerRecord SaveRecord(PlayerRecord record)
        {
            return _records[record.RecordId] = record;
        }
        public PlayerRecord RestoreRecord(Guid id)
        {
            if (!_records.ContainsKey(id)) return null;
            var rec = _records[id];
            return rec;
        }

        public MusicPlayer GetSession(DiscordGuild guild)
        {
            if (_sessions.ContainsKey(guild.Id))
            {
                return _sessions[guild.Id];
            }

            return null;
        }
        
        public GuildPlayerManager GetSessionNew(DiscordGuild guild)
        {
            if (_newSessions.ContainsKey(guild.Id))
            {
                return _newSessions[guild.Id];
            }
            else
            {
                var scope = _provider.CreateScope();
                return _newSessions[guild.Id] = scope.ServiceProvider.GetRequiredService<GuildPlayerManager>();
            }
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