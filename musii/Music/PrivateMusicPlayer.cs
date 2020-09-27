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
                    await Player.PlayAsync(track);
                    foreach (var k in q._list)
                    {
                        Player.Queue.Enqueue(k);
                    }
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
            if (Player != null && arg.GuildId == Guild && arg.ByRemote)
            {
                var q = Player.Queue;
                var track = Player.Track;
                var channel = Program._client.GetGuild(Guild).GetVoiceChannel(Player.VoiceChannel.Id);
                var tchannel = Player.TextChannel;
                try
                {
                    await Program.MusicNode.LeaveAsync(channel);
                    await Task.Delay(100);
                    var newPlayer = await Program.MusicNode.JoinAsync(channel, tchannel);
                    Player = newPlayer;
                    await Player.PlayAsync(track);
                    foreach (var k in q._list)
                    {
                        Player.Queue.Enqueue(k);
                    }
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
        private async Task MusicNodeOnTrackEnded(TrackEndedEventArgs arg)
        {
            if (arg.Player == Player)
            {
                if (_loop && arg.Reason == TrackEndReason.Finished)
                {
                    arg.Track.Position = TimeSpan.Zero;
                    Player.Queue.Enqueue(arg.Track);
                }

                if (Player.Queue.Count == 0)
                {
                    await Program.MusicNode.LeaveAsync(Player.VoiceChannel);
                    Player = null;
                }
                else
                {
                    Playback();
                }
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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
            if (Player == null || Player.PlayerState == PlayerState.Stopped)
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

            if (Player == null || Player.PlayerState == PlayerState.Stopped)
            {
                int cnt = 0;
                while (Player == null || Player.PlayerState == PlayerState.Stopped)
                {
                    Player = await Program.MusicNode.JoinAsync(voiceChannel, channel as ITextChannel);
                    await Task.Delay(100);
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
                await QueuePlaylist(keywords[0]);
            }
            else if (ResourceLocator.IsVideo(keywords))
            {
                await QueueVideo(keywords[0]);
            }
            else if (SpotifyController.ParsePlaylist(keywords[0]) != "")
            {
                var playlist = await SpotifyController.GetPlaylist(keywords[0]);
                if (playlist == null)
                {
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }

                await QueueSpotifyPlaylist(playlist);
            }
            else if (SpotifyController.ParseAlbum(keywords[0]) != "")
            {
                var album = await SpotifyController.GetAlbum(keywords[0]);
                if (album == null)
                {
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await QueueSpotifyAlbum(album);
            }
            else if (SpotifyController.ParseTrack(keywords[0]) != "")
            {
                var track = await SpotifyController.GetTrack(keywords[0]);
                if (track == null)
                {
                    await channel.SendMessageAsync(embed: TextInterface.NotFoundMessage(keywords[0]));
                }
                await QueueSpotifyTrack(track);
            }
            else
            {
                await SearchVideo(keywords);
            }
        }
        private YoutubeClient _guildMusicQuery = new YoutubeClient();
        private async Task QueuePlaylist(string link)
        {
            var playlist = PlaylistId.TryParse(link).Value;
            var videos = _guildMusicQuery.Playlists.GetVideosAsync(playlist.Value);
            int cnt = 0;

            await foreach (var vid in videos)
            {
                var ltrack = new LavaLazyTrack(vid.Title + " " + vid.Author, Program.MusicNode)
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

            await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueSpotifyPlaylist(FullPlaylist list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            foreach (var playlistTrack in tracks)
            {
                if (playlistTrack.Track is FullTrack track)
                {
                    var query = track.Name + " ";
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
            await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueSpotifyAlbum(FullAlbum list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            foreach (var track in tracks)
            {
                var query = track.Name + " ";
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
            await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueVideo(string link)
        {
            var id = VideoId.TryParse(link).Value;
            var vid = await _guildMusicQuery.Videos.GetAsync(id);
            var ltrack = new LavaLazyTrack(vid.Title + " " + vid.Author, Program.MusicNode)
            {
                OriginalTitle = vid.Title
            };

            await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, vid.Thumbnails.StandardResUrl));

            Player.Queue.Enqueue(ltrack);

            Playback();
        }
        private async Task QueueSpotifyTrack(FullTrack track)
        {
            var query = track.Name + " ";
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
                await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, track.PreviewUrl));
            }

            Player.Queue.Enqueue(ltrack);

            Playback();
        }
        private async Task SearchVideo(string[] keywords)
        {
            string query = string.Join(' ', keywords);
            var videos = _guildMusicQuery.Search.GetVideosAsync(query);
            try
            {
                var vid = await videos.FirstAsync().ConfigureAwait(false);

                var ltrack = new LavaLazyTrack(vid.Title + " " + vid.Author, Program.MusicNode)
                {
                    OriginalTitle = vid.Title
                };

                if (Player.Track != null) 
                {
                    await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player, vid.Thumbnails.StandardResUrl));
                }

                Player.Queue.Enqueue(ltrack);

                Playback();
            }
            catch
            {
                await Player.TextChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(query));
            }
        }

        private void Playback()
        {
            if (Player.PlayerState != PlayerState.Paused && Player.PlayerState != PlayerState.Playing)
            {
                if (Player.Queue.TryDequeue(out var val))
                {
                    _ = Player.PlayAsync(val).ConfigureAwait(false);
                }
            }
        }
    }
}
