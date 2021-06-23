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

        private IServiceProvider _provider;
        private RecordContext _context;
        public MusiiCore(IServiceProvider provider, RecordContext context)
        {
            _provider = provider;
            _context = context;
        }

        public PlayerRecord SaveRecord(PlayerRecord record)
        {
            lock (_context)
            {
                _context.Records.Add(record);
            }
            return record;
        }
        public PlayerRecord RestoreRecord(Guid id)
        {
            lock (_context)
            {
                var rec = _context.Records.Find(id);
                if (rec is null) return null;
                return rec;
            }
        }
        
        public MusiiGuild GetGuild(DiscordGuild guild)
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