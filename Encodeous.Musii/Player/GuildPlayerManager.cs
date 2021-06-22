using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Encodeous.Musii.Player
{
    public class GuildPlayerManager
    {
        public bool HasPlayer { get; private set; } = false;
        public NewMusicPlayer Player { get; private set; }
        public LavalinkGuildConnection Node { get; private set; } = null;
        public PlayerSessions Sessions { get; private set; }
        
        private DiscordClient _client;
        private ILogger _log;
        private SpotifyService _spotify;
        private SearchService _searcher = null;

        public GuildPlayerManager(DiscordClient client, ILogger<NewMusicPlayer> log, SpotifyService spotify, PlayerSessions sessions)
        {
            _client = client;
            _log = log;
            _spotify = spotify;
            Sessions = sessions;
        }

        public SearchService GetSearcher()
        {
            if (_searcher == null)
            {
                _searcher = new SearchService(_spotify, Node, new YoutubeService());
            }

            return _searcher;
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
                var plr = new NewMusicPlayer(_log, this, _client, nRec, voice, text, GetSearcher());
                Node.DiscordWebSocketClosed += (_, b) => plr.WsClosed(b);
                Node.PlaybackFinished += (_, b) => plr.PlaybackFinished(b);
                Node.TrackException += (_, b) => plr.TrackException(b);
                Node.TrackStuck += (_, b) => plr.TrackStuck(b);
                Node.PlayerUpdated += (_, b) => { plr.TrackUpdated(b); return Task.CompletedTask; };
                Player = plr;
                return true;
            }
            catch
            {
                try
                {
                    await Player.Stop();
                }
                catch
                {
                    
                }

                HasPlayer = false;
                Player = null;
                _searcher = null;
                await text.SendMessageAsync(Messagesv2.FailedToJoin(voice));
            }

            return false;
        }

        public async Task StopAsync(bool byPlayer = false)
        {
            if(!HasPlayer) throw new InvalidOperationException("Player is not active!");
            if (!byPlayer)
            {
                await Player.Stop();
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
    }
}