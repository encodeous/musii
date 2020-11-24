using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using musii.Music;
using musii.Utilities;
using SpotifyAPI.Web;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace musii.Service
{
    public class MusicPlayerService
    {
        private readonly LavaNode _node;
        private DiscordSocketClient _client;
        private readonly YoutubeClient _guildMusicQuery = new YoutubeClient();
        private bool _globalLock;

        public MusicPlayerService(LavaNode node, DiscordSocketClient client)
        {
            _node = node;
            _client = client;
            _client.Ready += OnReadyAsync;
            _client.Disconnected += ClientOnDisconnected;
        }

        private bool Disconnected = false;
        private Task ClientOnDisconnected(Exception arg)
        {
            Disconnected = true;
            return Task.CompletedTask;
        }

        // State Management Functions
        private async Task OnReadyAsync()
        {
            try
            {
                _globalLock = true;
                if (_node.IsConnected)
                {
                    await HandleNodeReconnection();
                }
                else
                {
                    await _node.ConnectAsync();
                    _node.OnTrackException += NodeOnTrackException;
                    _node.OnTrackEnded += NodeOnTrackEnded;
                    _node.OnTrackStuck += NodeOnTrackStuck;
                    _node.OnWebSocketClosed += NodeOnWebSocketClosed;
                    _node.OnPlayerUpdated += NodeOnPlayerUpdated;
                    _node.OnLog += arg =>
                    {
                        if (arg.Exception != null)
                        {
                            arg.Exception.Message.Log();
                            arg.Exception.StackTrace.Log();
                        }
                        arg.Message.Log();
                        return Task.CompletedTask;
                    };
                }
                $"Client Ready, Connected to Lavalink.".Log();
            }
            finally
            {
                _globalLock = false;
            }
        }

        private Task NodeOnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            arg.Player.QueueMessage?.ModifyAsync(x => { x.Embed = TextInterface.GetQueueMessage(arg.Player); });
            return Task.CompletedTask;
        }

        private async Task NodeOnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            if(Disconnected) return;
            if (arg.ByRemote)
            {
                var player = GetPlayer(_client.GetGuild(arg.GuildId));
                if (arg.Code == 4014)
                {
                    await player.TextChannel.SendMessageAsync(embed: TextInterface.StandardMessage(Config.Name + " has been disconnected by a moderator."));
                    try
                    {
                        await _node.LeaveAsync(player.VoiceChannel);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    $"Guild Session Disconnected by Remote, response: {arg.Code}, guildid: {arg.GuildId}, reason: {arg.Reason}.".Log();
                    // Discord Network Error
                    try
                    {
                        await ReconnectPlayerAsync(player);
                    }
                    catch
                    {
                        try
                        {
                            await _node.LeaveAsync(player.VoiceChannel);
                        }
                        catch
                        {

                        }
                        await player.TextChannel.SendMessageAsync(embed: TextInterface.StandardMessage("An unknown error occurred, the bot is not able to rejoin the voice channel."));
                    }
                }
            }
        }
        private Task NodeOnTrackStuck(TrackStuckEventArgs arg)
        {
            return arg.Player.PlayAsync(arg.Track);
        }
        private Task NodeOnTrackEnded(TrackEndedEventArgs arg)
        {
            if(arg.Reason == TrackEndReason.LoadFailed) return Task.CompletedTask;
            if (arg.Player.Looped && arg.Reason == TrackEndReason.Finished)
            {
                arg.Track.Position = TimeSpan.Zero;
                arg.Player.Queue.Enqueue(arg.Track);
            }

            if (arg.Reason == TrackEndReason.Finished && arg.Player.Queue.Count == 0)
            {
                arg.Player.TextChannel.SendMessageAsync(embed: TextInterface.FinishedPlayingMessage());
            }
            return CheckPlayerState(arg.Player);
        }

        private string _prevErrorTrack;
        private async Task NodeOnTrackException(TrackExceptionEventArgs arg)
        {
            if (arg.Track.Hash == _prevErrorTrack)
            {
                await CheckPlayerState(arg.Player);
                arg.ErrorMessage.Log();
                await arg.Player.TextChannel.SendMessageAsync(embed: TextInterface.InternalErrorMessage(arg.ErrorMessage));
            }
            else
            {
                _prevErrorTrack = arg.Track.Hash;
                await arg.Player.PlayAsync(arg.Track);
            }
        }
        private async Task HandleNodeReconnection()
        {
            try
            {
                _globalLock = true;
                var players = new List<(LinkedList<ILavaTrack>, ITextChannel, IVoiceChannel, bool, int, ILavaTrack)>();
                foreach (var k in _node._playerCache)
                {
                    try
                    {
                        await k.Value.TextChannel.SendMessageAsync(embed: TextInterface.ReconnectingMessage());
                    }
                    catch
                    {

                    }

                    var nq = new LinkedList<ILavaTrack>(k.Value.Queue);
                    players.Add((nq, k.Value.TextChannel, k.Value.VoiceChannel, k.Value.Looped, k.Value.Volume, k.Value.Track));
                }
                $"Disconnected, reconnecting {players.Count} guilds...".Log();
                await _node.DisconnectAsync();

                await Task.Delay(3000);

                await _node.ConnectAsync();
                foreach (var p in players)
                {
                    var player = new LavaPlayer(null, p.Item3, p.Item2)
                    {
                        Looped = p.Item4, Volume = p.Item5, Track = p.Item6, Queue = {InternalList = p.Item1}
                    };
                    await ReconnectPlayerAsync(player);
                }
            }
            finally
            {
                _globalLock = false;
                Disconnected = false;
            }
        }
        private async Task ReconnectPlayerAsync(LavaPlayer player)
        {
            var track = player.Track;
            var vc = player.VoiceChannel;
            var tc = player.TextChannel;
            var q = player.Queue;
            var loop = player.Looped;
            var vol = player.Volume;
            var paused = player.PlayerState == PlayerState.Paused;
            try
            {
                await _node.LeaveAsync(vc);
            }
            catch
            {

            }
            player = await _node.JoinAsync(vc, tc);
            player.Queue = q;
            player.Looped = loop;
            await player.UpdateVolumeAsync((ushort)vol);
            await player.PlayAsync(track);
            if (paused) await player.PauseAsync();
        }

        // Playback Functions
        public async Task CheckPlayerState(LavaPlayer player)
        {
            if (player.Queue.Count == 0)
            {
                await _node.LeaveAsync(player.VoiceChannel);
            }
            else
            {
                await Playback(player);
            }
        }
        private async Task Playback(LavaPlayer player)
        {
            if (player.PlayerState != PlayerState.Paused && player.PlayerState != PlayerState.Playing)
            {
                await NextTrackAsync(player);
            }
        }
        public async Task NextTrackAsync(LavaPlayer player)
        {
            if (player.Queue.TryDequeue(out var val))
            {
                if (val.Hash == null)
                {
                    await player.TextChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(val.Title));
                    await CheckPlayerState(player);
                }
                else
                {
                    await player.PlayAsync(val).ConfigureAwait(false);
                }
            }
        }
        public bool HasPlayer(IGuild guild)
        {
            return _node.HasPlayer(guild);
        }
        public LavaPlayer GetPlayer(IGuild guild)
        {
            return _node.GetPlayer(guild);
        }
        public async Task LoopMusicAsync(SocketCommandContext context)
        {
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var player = GetPlayer(context.Guild);
            player.Looped = !player.Looped;
            if (player.Looped)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.LoopOn());
            }
            else
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.LoopOff());
            }
        }
        public async Task SeekMusicAsync(SocketCommandContext context, TimeSpan delta, bool reverse)
        {
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var player = GetPlayer(context.Guild);
            var newTime = player.Track.Position;
            if (reverse)
            {
                newTime -= delta;
                if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;
            }
            else
            {
                newTime += delta;
                if (newTime > player.Track.Duration) newTime = player.Track.Duration - TimeSpan.FromSeconds(1);
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.SeekMessage(player.Track, newTime, player.PlayerState == PlayerState.Paused));
            await player.SeekAsync(newTime);
        }
        public async Task PauseMusicAsync(SocketCommandContext context)
        {
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var player = GetPlayer(context.Guild);
            if (player.PlayerState != PlayerState.Paused)
            {
                await player.PauseAsync();
                await context.Channel.SendMessageAsync(embed: TextInterface.PauseOn());
            }
            else
            {
                await player.ResumeAsync();
                await context.Channel.SendMessageAsync(embed: TextInterface.PauseOff());
            }
        }
        public async Task ShuffleMusicAsync(SocketCommandContext context)
        {
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            GetPlayer(context.Guild).Queue.Shuffle();
            await context.Channel.SendMessageAsync(embed: TextInterface.Shuffled()).ConfigureAwait(false);
        }
        public async Task SkipMusicAsync(int count, SocketCommandContext context)
        {
            var channel = context.Channel;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!")
                    .ConfigureAwait(false);
                return;
            }
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var player = GetPlayer(context.Guild);
            if (voiceChannel.Id != player.VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }
            if (count == 1)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.SkipSongMessage(player.Track, player.VoiceChannel));
                await player.StopAsync();
            }
            else if (count > 1)
            {
                int mcnt = Math.Min(count - 1, player.Queue.Count);
                await context.Channel.SendMessageAsync(embed: TextInterface.SkipSongsMessage(mcnt + 1));
                player.Queue.RemoveRange(0, mcnt);
                await player.StopAsync();
            }
        }
        public async Task ClearMusicAsync(SocketCommandContext context)
        {
            var channel = context.Channel;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var player = GetPlayer(context.Guild);
            if (voiceChannel.Id != player.VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }

            var songs = player.Queue.Count;
            player.Queue.Clear();
            await player.StopAsync();
            await context.Channel.SendMessageAsync(embed: TextInterface.QueueClearedMessage(songs + 1));
        }
        public async Task GetQueueAsync(SocketCommandContext context)
        {
            var channel = context.Channel;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }
            if (!HasPlayer(context.Guild))
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }

            var player = GetPlayer(context.Guild);
            player.QueueMessage = await context.Channel.SendMessageAsync(embed: TextInterface.GetQueueMessage(player));
        }
        public async Task PlaySongAsync(SocketCommandContext context, string[] keywords)
        {
            if (_globalLock) return;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await context.Channel.SendMessageAsync(
                    embed: TextInterface.StandardMessage(
                        "You must be in a voice channel to execute this command!"));
                return;
            }
            if (_node.HasPlayer(context.Guild))
            {
                var player = _node.GetPlayer(context.Guild);
                if (voiceChannel.Id != player.VoiceChannel.Id)
                {
                    await context.Channel.SendMessageAsync(
                        embed: TextInterface.StandardMessage(
                            "You must be in the music channel to execute this command!"));
                    return;
                }
                await PlayAsync(player, keywords);
            }
            else
            {
                var player = await _node.JoinAsync(voiceChannel, context.Channel as ITextChannel);
                await PlayAsync(player, keywords);
            }
        }
        private async Task PlayAsync(LavaPlayer player, string[] keywords)
        {
            var vc = player.VoiceChannel;
            var tc = player.TextChannel;
            if (ResourceLocator.IsPlaylist(keywords))
            {
                await tc.SendMessageAsync(embed: await QueuePlaylist(player, keywords[0]));
            }
            else if (ResourceLocator.IsVideo(keywords))
            {
                await QueueVideo(player, keywords[0]);
            }
            else if (SpotifyController.ParsePlaylist(keywords[0]) != "")
            {
                var playlist = await SpotifyController.GetPlaylist(keywords[0]);
                if (playlist == null)
                {
                    await CheckPlayerState(player);
                    await tc.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }

                await tc.SendMessageAsync(embed: await QueueSpotifyPlaylist(player, playlist));
            }
            else if (SpotifyController.ParseAlbum(keywords[0]) != "")
            {
                var album = await SpotifyController.GetAlbum(keywords[0]);
                if (album == null)
                {
                    await CheckPlayerState(player);
                    await tc.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await tc.SendMessageAsync(embed: await QueueSpotifyAlbum(player, album));
            }
            else if (SpotifyController.ParseTrack(keywords[0]) != "")
            {
                var track = await SpotifyController.GetTrack(keywords[0]);
                if (track == null)
                {
                    await CheckPlayerState(player);
                    await tc.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await QueueSpotifyTrack(player, track);
            }
            else
            {
                await SearchVideo(player, keywords);
            }
        }
        private async Task<Embed> QueuePlaylist(LavaPlayer player, string link)
        {
            try
            {
                var playlist = PlaylistId.TryParse(link).Value;
                var videos = _guildMusicQuery.Playlists.GetVideosAsync(playlist.Value);
                int cnt = 0;
                await foreach (var vid in videos)
                {
                    var ltrack = new LavaLazyTrack(vid.Id.Value, _node) {OriginalTitle = vid.Title};
                    player.Queue.Enqueue(ltrack);
                    if (++cnt >= Config.MaxPlaylistLength) break;
                }
                await Playback(player);
                return TextInterface.QueuedSongsMessage(cnt);
            }
            catch
            {
                await CheckPlayerState(player);
                return TextInterface.NotFoundMessage(link);
            }
        }
        private async Task<Embed> QueueSpotifyPlaylist(LavaPlayer player, FullPlaylist list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            foreach (var playlistTrack in tracks)
            {
                if (playlistTrack.Track is FullTrack track)
                {
                    var query = "ytsearch:" + track.Name + " ";
                    foreach (var a in track.Artists) query += a.Name + " ";
                    var ltrack = new LavaLazyTrack(query, _node) {OriginalTitle = track.Name};
                    player.Queue.Enqueue(ltrack);
                    if (++cnt >= Config.MaxPlaylistLength) break;
                }
            }
            await Playback(player);
            return TextInterface.QueuedSongsMessage(cnt);
        }
        private async Task<Embed> QueueSpotifyAlbum(LavaPlayer player, FullAlbum list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;
            foreach (var track in tracks)
            {
                var query = "ytsearch:" + track.Name + " ";
                foreach (var a in track.Artists) query += a.Name + " ";

                var ltrack = new LavaLazyTrack(query, _node) {OriginalTitle = track.Name};
                player.Queue.Enqueue(ltrack);
                if (++cnt >= Config.MaxPlaylistLength) break;
            }
            await Playback(player);
            return TextInterface.QueuedSongsMessage(cnt);
        }
        private async Task QueueVideo(LavaPlayer player, string link)
        {
            try
            {
                var id = VideoId.TryParse(link).Value;
                var vid = await _guildMusicQuery.Videos.GetAsync(id);
                var ltrack = new LavaLazyTrack(vid.Id.Value, _node) {OriginalTitle = vid.Title};
                await player.TextChannel.SendMessageAsync(embed:
                    TextInterface.QueuedSongMessage(ltrack, player));
                player.Queue.Enqueue(ltrack);
                await Playback(player);
            }
            catch
            {
                await CheckPlayerState(player);
                await player.TextChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(link));
            }
        }
        private async Task QueueSpotifyTrack(LavaPlayer player, FullTrack track)
        {
            var query = "ytsearch:" + track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            var ltrack = new LavaLazyTrack(query, _node) {OriginalTitle = track.Name};

            await player.TextChannel.SendMessageAsync(embed: 
                TextInterface.QueuedSongMessage(ltrack, player));

            player.Queue.Enqueue(ltrack);
            await Playback(player);
        }
        private async Task SearchVideo(LavaPlayer player, string[] keywords)
        {
            string query = string.Join(' ', keywords);
            try
            {
                var videos = _guildMusicQuery.Search.GetVideosAsync(query);
                var vid = await videos.FirstAsync().ConfigureAwait(false);
                var ltrack = new LavaLazyTrack(vid.Id.Value, _node) {OriginalTitle = vid.Title};

                await player.TextChannel.SendMessageAsync(embed: 
                    TextInterface.QueuedSongMessage(ltrack, player));

                player.Queue.Enqueue(ltrack);
                await Playback(player);
            }
            catch
            {
                await CheckPlayerState(player);
                await player.TextChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(query));
            }
        }
    }
}
