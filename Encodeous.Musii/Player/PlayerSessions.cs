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
        private ConcurrentDictionary<ulong, MusiiGuildManager> _newSessions = new();
        private ConcurrentDictionary<Guid, PlayerRecord> _records = new();

        private IServiceProvider _provider;
        public PlayerSessions(IServiceProvider provider)
        {
            _provider = provider;
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
        
        public MusiiGuildManager GetSessionNew(DiscordGuild guild)
        {
            if (_newSessions.ContainsKey(guild.Id))
            {
                return _newSessions[guild.Id];
            }
            else
            {
                var scope = _provider.CreateScope();
                return _newSessions[guild.Id] = scope.ServiceProvider.GetRequiredService<MusiiGuildManager>();
            }
        }
    }
}