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
using musii.Deprecated.Musiiv2;
using musii.Utilities;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
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
        public bool Locked = false;

        public PrivateMusicPlayer()
        {
            Program.MusicNode.OnTrackStuck += MusicNodeOnTrackStuck;
            Program.MusicNode.OnPlayerUpdated += MusicNodeOnPlayerUpdated;
            Program.MusicNode.OnTrackEnded += MusicNodeOnTrackEnded;
            Program.MusicNode.OnTrackException += MusicNodeOnTrackException;
            Program.MusicNode.OnTrackStarted += MusicNodeOnTrackStarted;
            Program.MusicNode.OnWebSocketClosed += MusicNodeOnWebSocketClosed;
        }

        private Task MusicNodeOnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task MusicNodeOnTrackStarted(TrackStartEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task MusicNodeOnTrackException(TrackExceptionEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task MusicNodeOnTrackEnded(TrackEndedEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task MusicNodeOnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task MusicNodeOnTrackStuck(TrackStuckEventArgs arg)
        {

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

        public async Task ShuffleMusicAsync(SocketCommandContext context)
        {
            if (Player == null)
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
            if (Player == null)
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
                await Player.StopAsync();
            }
            else if (count > 1)
            {
                int mcnt = Math.Min(count-1, Player.Queue.Count);
                await context.Channel.SendMessageAsync(embed: TextInterface.QueueClearedMessage(mcnt+1));
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
            if (Player == null)
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
            if (Player == null)
            {
                await context.Channel.SendMessageAsync(embed: TextInterface.NoMusic()).ConfigureAwait(false);
                return;
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.GetQueueMessage(Player, _loop));
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
            if (Player == null)
            {
                Player = await Program.MusicNode.JoinAsync(voiceChannel, channel as ITextChannel);
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

            await foreach (var playlistVideo in videos)
            {
                var ltrack = new LavaLazyTrack(playlistVideo.Title + " " + playlistVideo.Author, Program.MusicNode);
                ltrack.ThumbnailUrl = playlistVideo.Thumbnails.StandardResUrl;
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
                    var ltrack = new LavaLazyTrack(track.Name + " " + track.Album.Name, Program.MusicNode)
                    {
                        ThumbnailUrl = track.PreviewUrl
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
                var ltrack = new LavaLazyTrack(track.Name, Program.MusicNode)
                {
                    ThumbnailUrl = track.PreviewUrl
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
            var ltrack = new LavaLazyTrack(vid.Title + " " + vid.Author, Program.MusicNode);
            ltrack.ThumbnailUrl = vid.Thumbnails.StandardResUrl;

            if (Player.Queue.Any())
            {
                await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player));
            }

            Player.Queue.Enqueue(ltrack);

            Playback();
        }
        private async Task QueueSpotifyTrack(FullTrack track)
        {
            var ltrack = new LavaLazyTrack(track.Name, Program.MusicNode);
            ltrack.ThumbnailUrl = track.PreviewUrl;

            if (Player.Queue.Any())
            {
                await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player));
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

                var ltrack = new LavaLazyTrack(vid.Title + " " + vid.Author, Program.MusicNode);
                ltrack.ThumbnailUrl = vid.Thumbnails.StandardResUrl;

                if (Player.Queue.Any())
                {
                    await Player.TextChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(ltrack, Player));
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
            if (Player.PlayerState == PlayerState.Stopped)
            {
                if (Player.Queue.TryDequeue(out var val))
                {
                    _ = Player.PlayAsync(val).ConfigureAwait(false);
                }
            }
        }
    }
}
