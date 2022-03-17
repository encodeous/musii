using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Encodeous.Musii.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encodeous.Musii.Core
{
    /// <summary>
    /// Manages the "playing" sessions
    /// </summary>
    public class MusiiCore
    {
        private ConcurrentDictionary<ulong, MusiiGuild> _guilds = new();

        private IServiceProvider _provider;
        private IConfiguration _config;
        private bool _requireAuthorization;
        private HashSet<ulong> _authorizedGuilds;
        public MusiiCore(IServiceProvider provider, IConfiguration config)
        {
            _provider = provider;
            _config = config;
            _requireAuthorization = bool.TryParse(config["musii:RequireGuildAuthorization"], out var val);
            _requireAuthorization = _requireAuthorization && val;
            if (_requireAuthorization)
            {
                _authorizedGuilds = new HashSet<ulong>();
                if (File.Exists("authorized.json"))
                {
                    _authorizedGuilds =
                        JsonSerializer.Deserialize<HashSet<ulong>>(File.ReadAllText("authorized.json"));
                }
            }
        }

        public async ValueTask<bool> CheckAuthorization(CommandContext ctx)
        {
            if (_authorizedGuilds.Contains(ctx.Guild.Id))
            {
                return true;
            }

            await ctx.RespondAsync(Messages.NotAuthorized(ctx.Guild.Id));
            return false;
        }

        public void AuthorizeGuild(CommandContext ctx, ulong guild)
        {
            lock (_authorizedGuilds)
            {
                _authorizedGuilds.Add(guild);
                File.WriteAllText("authorized.json", JsonSerializer.Serialize(_authorizedGuilds));
            }
        }
        
        public void RevokeGuild(CommandContext ctx, ulong guild)
        {
            lock (_authorizedGuilds)
            {
                _authorizedGuilds.Remove(guild);
                File.WriteAllText("authorized.json", JsonSerializer.Serialize(_authorizedGuilds));
            }
        }

        public PlayerRecord SaveRecord(PlayerRecord record)
        {
            using var context = new RecordContext();
            context.Records.Add(record);
            return record;
        }
        public PlayerRecord RestoreRecord(Guid id)
        {
            using var context = new RecordContext();
            var rec = context.Records.Find(id);
            return rec;
        }
        
        public MusiiGuild GetMusiiGuild(DiscordGuild guild)
        {
            if (_guilds.ContainsKey(guild.Id))
            {
                return _guilds[guild.Id];
            }

            var scope = _provider.CreateScope();
            return _guilds[guild.Id] = scope.ServiceProvider.GetRequiredService<MusiiGuild>();
        }
    }
}