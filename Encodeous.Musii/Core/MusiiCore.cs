using System;
using System.Collections.Concurrent;
using DSharpPlus.Entities;
using Encodeous.Musii.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Encodeous.Musii.Core
{
    /// <summary>
    /// Manages the "playing" sessions
    /// </summary>
    public class MusiiCore
    {
        private ConcurrentDictionary<ulong, MusiiGuild> _newSessions = new();
        private ConcurrentDictionary<Guid, PlayerRecord> _records = new();

        private IServiceProvider _provider;
        public MusiiCore(IServiceProvider provider)
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
        
        public MusiiGuild GetSessionNew(DiscordGuild guild)
        {
            if (_newSessions.ContainsKey(guild.Id))
            {
                return _newSessions[guild.Id];
            }
            else
            {
                var scope = _provider.CreateScope();
                return _newSessions[guild.Id] = scope.ServiceProvider.GetRequiredService<MusiiGuild>();
            }
        }
    }
}