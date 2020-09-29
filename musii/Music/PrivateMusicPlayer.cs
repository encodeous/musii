using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using musii.Utilities;
using SpotifyAPI.Web;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace musii.Music
{
    class PrivateMusicPlayer
    {
        public LavaPlayer Player;
        private bool _loop = false;
        private bool _pause = false;
        public bool Locked = false;
        public ulong Guild;

        public PrivateMusicPlayer(ulong guild)
        {
            Guild = guild;
            Program.MusicNode.OnTrackStarted += MusicNodeOnTrackStarted;
            Program.MusicNode.OnTrackEnded += MusicNodeOnTrackEnded;
            Program.MusicNode.OnWebSocketClosed += MusicNodeOnWebSocketClosed;
            Program.MusicNode.OnTrackStuck += MusicNodeOnTrackStuck;
            Program.MusicNode.OnTrackException += MusicNodeOnTrackException;
        }

        private async Task MusicNodeOnTrackException(TrackExceptionEventArgs arg)
        {
            if (arg.Player == Player)
            {
                arg.ErrorMessage.Log();
                var q = Player.Queue;
                var channel = Player.VoiceChannel;
                var tchannel = Player.TextChannel;
                await Program.MusicNode.LeaveAsync(channel);
                await Task.Delay(100);
                try
                {
                    await tchannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(Player.Track.Title));
                    var newPlayer = await Program.MusicNode.JoinAsync(channel, tchannel);
                    Player = newPlayer;
                    foreach (var k in q.InternalList)
                    {
                        Player.Queue.Enqueue(k);
                    }

                    await CheckLeave();
                }
                catch
                {

                }
            }
        }

        private async Task MusicNodeOnTrackStuck(TrackStuckEventArgs arg)
        {
            if (arg.Player == Player)
            {
                var q = Player.Queue;
                var track = Player.Track;
                var channel = Player.VoiceChannel;
                var tchannel = Player.TextChannel;
                await Program.MusicNode.LeaveAsync(channel);
                await Task.Delay(100);
                try
                {
                    var newPlayer = await Program.MusicNode.JoinAsync(channel, tchannel);
                    Player = newPlayer;
                    Player.Queue.Enqueue(track);
                    foreach (var k in q.InternalList)
                    {
                        Player.Queue.Enqueue(k);
                    }
                    await Playback();
                }
                catch
                {

                }
            }
        }

        private async Task MusicNodeOnTrackStarted(TrackStartEventArgs arg)
        {
            if (arg.Player == Player)
            {
                await Player.TextChannel.SendMessageAsync(
                    embed: TextInterface.NowPlayingMessage(arg.Track, Player.VoiceChannel));
            }
        }

        private async Task MusicNodeOnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            if (Player != null && arg.GuildId == Guild)
            {
                
                if (arg.Code == 4014 || arg.Code == 4011)
                {
                    await Player.TextChannel.SendMessageAsync(Config.Name + " has been disconnected by a moderator.");
                    try
                    {
                        await Program.MusicNode.LeaveAsync(Player.VoiceChannel);
                    }
                    catch
                    {

                    }

                    Player = null;
                }
                else if(arg.ByRemote)
                {
                    // Discord Network Error
                    var q = Player.Queue;
                    var track = Player.Track;
                    var channel = Program._client.GetGuild(Guild).GetVoiceChannel(Player.VoiceChannel.Id);
                    var tchannel = Player.TextChannel;
                    try
                    {
                        await Program.MusicNode.LeaveAsync(channel);
                        await Task.Delay(1000);
                        var newPlayer = await Program.MusicNode.JoinAsync(channel, tchannel);
                        Player = newPlayer;
                        Player.Queue.Enqueue(track);
                        foreach (var k in q.InternalList)
                        {
                            Player.Queue.Enqueue(k);
                        }

                        await Playback();
                    }
                    catch
                    {
                        try
                        {
                            await Program.MusicNode.LeaveAsync(channel);
                        }
                        catch
                        {

                        }
                        Player = null;
                        await tchannel.SendMessageAsync(
                            "An unknown error occurred, the bot is not able to join the voice channel.");
                    }
                }
            }
        }
        private async Task MusicNodeOnTrackEnded(TrackEndedEventArgs arg)
        {
            if (arg.Player == Player)
            {
                if (_loop && arg.Reason == TrackEndReason.Finished)
                {
                    arg.Track.Position = TimeSpan.Zero;
                    Player.Queue.Enqueue(arg.Track);
                }

                await CheckLeave();
            }
        }

        public async Task CheckLeave()
        {
            if (Player.Queue.Count == 0)
            {
                await Player.TextChannel.SendMessageAsync(embed: TextInterface.FinishedPlayingMessage());
                await Program.MusicNode.LeaveAsync(Player.VoiceChannel);
                Player = null;
            }
            else
            {
                await Playback();
            }
        }

        public async Task LoopMusicAsync(SocketCommandContext context)
        {
            _loop = !_loop;
            if (_loop)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            var newTime = Player.Track.Position;
            if (reverse)
            {
                newTime -= delta;
                if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;
            }
            else
            {
                newTime += delta;
                if (newTime > Player.Track.Duration) newTime = Player.Track.Duration - TimeSpan.FromSeconds(1);
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.SeekMessage(Player.Track, newTime, _pause));
            await Player.SeekAsync(newTime);
        }
        public async Task PauseMusicAsync(SocketCommandContext context)
        {
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            _pause = !_pause;
            if (_pause)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.PauseOn());
                await Player.PauseAsync();
            }
            else
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.PauseOff());
                await Player.ResumeAsync();
            }
        }
        public async Task ShuffleMusicAsync(SocketCommandContext context)
        {
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            Player.Queue.Shuffle();
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            if (voiceChannel.Id != Player.VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }

            if (count == 1)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.SkipSongMessage(Player.Track, Player.VoiceChannel));
                await Player.StopAsync();
            }
            else if (count > 1)
            {
                int mcnt = Math.Min(count-1, Player.Queue.Count);
                await context.Channel.SendMessageAsync(embed: TextInterface.SkipSongsMessage(mcnt+1));
                Player.Queue.RemoveRange(0, mcnt);
                await Player.StopAsync();
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            if (voiceChannel.Id != Player.VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }

            var songs = Player.Queue.Count;
            Player.Queue.Clear();
            await Player.StopAsync();
            await context.Channel.SendMessageAsync(embed: TextInterface.QueueClearedMessage(songs+1));
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.GetQueueMessage(Player, _loop, _pause));
        }
        public async Task PlayAsync(SocketCommandContext context, string[] keywords)
        {
            var channel = context.Channel;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;

            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }

            if (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
            {
                int cnt = 0;
                try
                {
                    await Program.MusicNode.LeaveAsync(voiceChannel);
                }
                catch
                {

                }
                while (Player == null || Player.PlayerState == PlayerState.Stopped || Player.PlayerState == PlayerState.Disconnected)
                {
                    Program.MusicNode._playerCache.TryRemove(context.Guild.Id, out _);
                    Player = await Program.MusicNode.JoinAsync(voiceChannel, channel as ITextChannel);
                    await Task.Delay(1000);
                    cnt++;
                    if (cnt >= 5)
                    {
                        await channel.SendMessageAsync("Unable to join channel, please try again later.");
                        try
                        {
                            await Program.MusicNode.LeaveAsync(voiceChannel);
                        }
                        catch
                        {

                        }
                        Program.MusicNode._playerCache.TryRemove(context.Guild.Id, out _);
                        Player = null;
                        return;
                    }
                }
            }
            else if (voiceChannel.Id != Player.VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You cannot play music in this channel while it is playing in another channel!").ConfigureAwait(false);
                return;
            }
            if (ResourceLocator.IsPlaylist(keywords))
            {
                await QueuePlaylist(context, keywords[0]);
            }
            else if (ResourceLocator.IsVideo(keywords))
            {
                await QueueVideo(context, keywords[0]);
            }
            else if (SpotifyController.ParsePlaylist(keywords[0]) != "")
            {
                var playlist = await SpotifyController.GetPlaylist(keywords[0]);
                if (playlist == null)
                {
                    await CheckLeave();
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }

                await QueueSpotifyPlaylist(context, playlist);
            }
            else if (SpotifyController.ParseAlbum(keywords[0]) != "")
            {
                var album = await SpotifyController.GetAlbum(keywords[0]);
                if (album == null)
                {
                    await CheckLeave();
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await QueueSpotifyAlbum(context, album);
            }
            else if (SpotifyController.ParseTrack(keywords[0]) != "")
            {
                var track = await SpotifyController.GetTrack(keywords[0]);
                if (track == null)
                {
                    await CheckLeave();
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await QueueSpotifyTrack(context, track);
            }
            else
            {
                await SearchVideo(context, keywords);
            }
        }
        private YoutubeClient _guildMusicQuery = new YoutubeClient();
        private async Task QueuePlaylist(SocketCommandContext context, string link)
        {
            try
            {
                var playlist = PlaylistId.TryParse(link).Value;
                var videos = _guildMusicQuery.Playlists.GetVideosAsync(playlist.Value);
                int cnt = 0;

                await foreach (var vid in videos)
                {
                    var ltrack = new LavaLazyTrack(vid.Id.Value, Program.MusicNode)
                    {
                        OriginalTitle = vid.Title
                    };
                    Player.Queue.Enqueue(ltrack);
                    cnt++;
                    if (cnt >= 500)
                    {
                        break;
                    }
                }

                await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

                await Playback();
            }
            catch
            {
                await CheckLeave();
                await context.Channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(link));
            }
        }
        private async Task QueueSpotifyPlaylist(SocketCommandContext context, FullPlaylist list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            foreach (var playlistTrack in tracks)
            {
                if (playlistTrack.Track is FullTrack track)
                {
                    var query = "ytsearch:" + track.Name + " ";
                    foreach (var a in track.Artists)
                    {
                        query += a.Name + " ";
                    }
                    var ltrack = new LavaLazyTrack(query, Program.MusicNode)
                    {
                        OriginalTitle = track.Name
                    };
                    Player.Queue.Enqueue(ltrack);
                    cnt++;
                    if (cnt >= 500)
                    {
                        break;
                    }
                }
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            await Playback();
        }
        private async Task QueueSpotifyAlbum(SocketCommandContext context, FullAlbum list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            foreach (var track in tracks)
            {
                var query = "ytsearch:" + track.Name + " ";
                foreach (var a in track.Artists)
                {
                    query += a.Name + " ";
                }
                var ltrack = new LavaLazyTrack(query, Program.MusicNode)
                {
                    OriginalTitle = track.Name
                };
                Player.Queue.Enqueue(ltrack);
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            await Playback();
        }
        private async Task QueueVideo(SocketCommandContext context, string link)
        {
            try
            {
                var id = VideoId.TryParse(link).Value;
                var vid = await _guildMusicQuery.Videos.GetAsync(id);
                var ltrack = new LavaLazyTrack(vid.Id.Value, Program.MusicNode)
                {
                    OriginalTitle = vid.Title
                };

                await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, vid.Thumbnails.StandardResUrl));

                Player.Queue.Enqueue(ltrack);

                await Playback();
            }
            catch
            {
                await CheckLeave();
                await context.Channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(link));
            }
        }
        private async Task QueueSpotifyTrack(SocketCommandContext context, FullTrack track)
        {
            var query = "ytsearch:" + track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            var ltrack = new LavaLazyTrack(query, Program.MusicNode)
            {
                OriginalTitle = track.Name
            };

            if (Player.Track != null)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, track.PreviewUrl));
            }

            Player.Queue.Enqueue(ltrack);

            await Playback();
        }
        private async Task SearchVideo(SocketCommandContext context, string[] keywords)
        {
            string query = string.Join(' ', keywords);
            try
            {
                var videos = _guildMusicQuery.Search.GetVideosAsync(query);
                var vid = await videos.FirstAsync().ConfigureAwait(false);

                var ltrack = new LavaLazyTrack(vid.Id.Value, Program.MusicNode)
                {
                    OriginalTitle = vid.Title
                };

                if (Player.Track != null) 
                {
                    await context.Channel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, vid.Thumbnails.StandardResUrl));
                }

                Player.Queue.Enqueue(ltrack);

                await Playback();
            }
            catch
            {
                await CheckLeave();
                await context.Channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(query));
            }
        }

        private async Task Playback()
        {
            if (Player.PlayerState != PlayerState.Paused && Player.PlayerState != PlayerState.Playing)
            {
                if (Player.Queue.TryDequeue(out var val))
                {
                    if (val.Hash == null)
                    {
                        await Player.TextChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(val.Title));
                        await CheckLeave();
                    }
                    else
                    {
                        _ = Player.PlayAsync(val).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
