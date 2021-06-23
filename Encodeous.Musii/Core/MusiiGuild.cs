using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Encodeous.Musii.Player;
using Encodeous.Musii.Search;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace Encodeous.Musii.Core
{
    public class MusiiGuild
    {
        public bool HasPlayer { get; set; }
        public MusiiPlayer Player { get; private set; }
        public LavalinkGuildConnection Node { get; private set; }
        public MusiiCore Core { get; }
        
        private DiscordClient _client;
        private ILogger _log;
        private SpotifyService _spotify;
        private SearchService _search;
        private DiscordChannel _traceLog;
        private HashSet<TraceSource> _traceFilter = new();
        private IConfiguration _config;

        public MusiiGuild(DiscordClient client, ILogger<MusiiPlayer> log, SpotifyService spotify, MusiiCore core, IConfiguration config)
        {
            _client = client;
            _log = log;
            _spotify = spotify;
            Core = core;
            _config = config;
        }

        public void SetTraceDestination(DiscordChannel channel, TraceSource[] filter)
        {
            _traceLog = channel;
            _traceFilter = filter.ToHashSet();
        }

        public async Task Trace(TraceSource source, dynamic traceData)
        {
            if (_traceLog is not null && _traceFilter.Contains(source))
            {
                var eb = new DiscordEmbedBuilder()
                    .WithFooter($"TRACE - {source}")
                    .WithColor(DiscordColor.Blurple)
                    .WithDescription($"```json\n{JsonConvert.SerializeObject(traceData, Formatting.Indented)}\n```");
                await _traceLog.SendMessageAsync(eb);
            }
        }

        public SearchService GetSearcher()
        {
            if (_search == null)
            {
                _search = new SearchService(_spotify, Node, new YoutubeService());
            }

            return _search;
        }
        
        public async Task<bool> CreatePlayerAsync(DiscordChannel voice, DiscordChannel text, PlayerRecord record = null)
        {
            if (HasPlayer) throw new InvalidOperationException("Player is already active!");
            HasPlayer = true;
            try
            {
                var conn = _client.GetLavalink().GetIdealNodeConnection();
                var tsk = voice.ConnectAsync(conn);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token;
                await tsk.WaitAsync(cts);
                if (cts.IsCancellationRequested) throw new Exception("Timed out");
                Node = conn.GetGuildConnection(voice.Guild);
                PlayerRecord nRec = record;
                if (nRec == null) nRec = new PlayerRecord();
                var plr = new MusiiPlayer(_log, this, _client, nRec, voice, text,
                    TimeSpan.FromSeconds(double.Parse(_config["musii:UserUnpinnnedLeaveTimeoutSeconds"])),
                    int.Parse(_config["musii:DefaultQueueLength"]),
                    TimeSpan.FromSeconds(double.Parse(_config["musii:InteractQueueTimeoutSeconds"]))
                    );
                Node.DiscordWebSocketClosed += (_, b) => plr.WsClosed(b);
                Node.PlaybackFinished += (_, b) => plr.PlaybackFinished(b);
                Node.TrackException += (_, b) => plr.TrackException(b);
                Node.TrackStuck += (_, b) => plr.TrackStuck(b);
                Node.PlayerUpdated += (_, b) => plr.TrackUpdated(b);
                Player = plr;
                return true;
            }
            catch
            {
                try
                {
                    await Player.StopAsync();
                }
                catch
                {
                    
                }

                HasPlayer = false;
                Player = null;
                _search = null;
                await text.SendMessageAsync(Messages.FailedToJoin(voice));
            }

            return false;
        }

        public async Task StopAsync(bool byPlayer = false)
        {
            if(!HasPlayer) throw new InvalidOperationException("Player is not active!");
            if (!byPlayer)
            {
                await Player.StopAsync();
            }
            else
            {
                try
                {
                    await Node.DisconnectAsync();
                }
                catch
                {
                    
                }

                Player = null;
                HasPlayer = false;
            }
        }

        public Task<LavalinkTrack> ResolveTrackAsync(BaseMusicSource source)
        {
            return source.GetTrack(Node);
        }
    }
}