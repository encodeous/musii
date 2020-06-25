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
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace musii.Music
{
    class PrivateMusicPlayer
    {
        public ISocketMessageChannel TextMessageChannel;
        public IVoiceChannel VoiceChannel;
        private PrivateMusic _musicStructure;
        private YoutubeClient _guildMusicQuery;
        private SemaphoreSlim _playbackSlim = new SemaphoreSlim(1);
        private IAudioClient _guildAudioClient;
        private AudioOutStream _guildAudioStream;
        public PrivateMusicPlayer()
        {
            _musicStructure = new PrivateMusic();
            _guildMusicQuery = new YoutubeClient();
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
            if (VoiceChannel == null)
            {
                await channel.SendMessageAsync($"There is no music playing").ConfigureAwait(false);
                return;
            }
            if (voiceChannel.Id != VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }


            var songs = _musicStructure.SkipSong(count) + 1;

            if (count > 1)
            {
                if (_musicStructure.PeekNext() == null)
                {
                    await context.Channel.SendMessageAsync(embed: TextInterface.QueueClearedMessage(songs));
                }
                else
                {
                    await context.Channel.SendMessageAsync(embed: TextInterface.SkipSongsMessage(songs));
                }
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
            if (VoiceChannel == null)
            {
                await channel.SendMessageAsync($"There is no music playing").ConfigureAwait(false);
                return;
            }
            if (voiceChannel.Id != VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You are not in the music channel!").ConfigureAwait(false);
                return;
            }

            var songs = _musicStructure.SkipSong(_musicStructure.MusicPlaylist.Count + 2);
            await context.Channel.SendMessageAsync(embed: TextInterface.QueueClearedMessage(songs));
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
            if (VoiceChannel == null)
            {
                await channel.SendMessageAsync($"There is no music playing").ConfigureAwait(false);
                return;
            }
            await context.Channel.SendMessageAsync(embed: TextInterface.GetQueueMessage(_musicStructure));
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
            if (VoiceChannel == null)
            {
                VoiceChannel = voiceChannel;
                TextMessageChannel = channel;
            }
            else if (voiceChannel.Id != VoiceChannel.Id)
            {
                await channel.SendMessageAsync($"You cannot play music in this channel while it is playing in another channel!").ConfigureAwait(false);
                return;
            }

            VoiceChannel = voiceChannel;

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
        private async Task QueuePlaylist(string link)
        {
            var playlist = PlaylistId.TryParse(link).Value;
            var videos = _guildMusicQuery.Playlists.GetVideosAsync(playlist.Value);
            int cnt = 0;

            List<IMusicPlayback> requests = new List<IMusicPlayback>();

            await foreach (var playlistVideo in videos)
            {
                requests.Add(new YoutubePlayback(playlistVideo));
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }

            _musicStructure.QueueSong(requests);

            await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueSpotifyPlaylist(FullPlaylist list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            List<IMusicPlayback> requests = new List<IMusicPlayback>();

            foreach (var playlistTrack in tracks)
            {
                if (playlistTrack.Track is FullTrack track)
                {
                    requests.Add(new SpotifyPlayback(track));
                    cnt++;
                    if (cnt >= 500)
                    {
                        break;
                    }
                }
            }

            _musicStructure.QueueSong(requests);

            await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueSpotifyAlbum(FullAlbum list)
        {
            var tracks = list.Tracks.Items;
            int cnt = 0;

            List<IMusicPlayback> requests = new List<IMusicPlayback>();

            foreach (var track in tracks)
            {
                requests.Add(new SpotifyPlayback(track));
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }

            _musicStructure.QueueSong(requests);

            await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongsMessage(cnt));

            Playback();
        }
        private async Task QueueVideo(string link)
        {
            var id = VideoId.TryParse(link).Value;

            var pb = YoutubePlayback.Parse(id.Value);

            _musicStructure.QueueSong(new []{ pb });

            if (_musicStructure.CurrentSong() != null)
            {
                await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(pb, _musicStructure));
            }

            Playback();
        }
        private async Task QueueSpotifyTrack(FullTrack track)
        {
            var pb = new SpotifyPlayback(track);

            _musicStructure.QueueSong(new[] { pb });

            if (_musicStructure.CurrentSong() != null)
            {
                await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(pb, _musicStructure));
            }

            Playback();
        }
        private async Task SearchVideo(string[] keywords)
        {
            string query = string.Join(' ', keywords);
            var videos = _guildMusicQuery.Search.GetVideosAsync(query);
            try
            {
                var id = await videos.FirstAsync().ConfigureAwait(false);
                var pb = new YoutubePlayback(id);
                _musicStructure.QueueSong(new[] { pb });

                if (_musicStructure.CurrentSong() != null)
                {
                    await TextMessageChannel.SendMessageAsync(embed: TextInterface.QueuedSongMessage(pb, _musicStructure));
                }

                Playback();
            }
            catch
            {
                await TextMessageChannel.SendMessageAsync(embed: TextInterface.NotFoundMessage(query));
            }
        }

        private void Playback()
        {
            if (_playbackSlim.CurrentCount > 0)
            {
                TaskUtils.Forget(PlayFunction);
            }
        }

        private async Task<AudioOutStream> GetGuildStream()
        {
            if (_guildAudioClient == null ||
                _guildAudioClient.ConnectionState != ConnectionState.Connected ||
                _guildAudioStream == null)
            {
                if (_guildAudioStream != null) await _guildAudioStream.DisposeAsync();
                if (_guildAudioClient == null)
                {
                    _guildAudioClient = await VoiceChannel.ConnectAsync();
                }

                if (_guildAudioClient.ConnectionState == ConnectionState.Disconnected)
                {
                    await _guildAudioClient.StopAsync();
                    _guildAudioClient = await VoiceChannel.ConnectAsync();
                }
                
                _guildAudioStream = _guildAudioClient.CreatePCMStream(AudioApplication.Music);
            }

            return _guildAudioStream;
        }

        private async Task PlayFunction()
        {
            await _playbackSlim.WaitAsync();

            while (true)
            {
                var currentSong = _musicStructure.PlayNext();
                if (currentSong == null) break;
                Stream yStream = null;
                try
                {
                    yStream = currentSong.GetStream();
                }
                catch(Exception e)
                {
                    await TextMessageChannel.SendMessageAsync(
                        embed: TextInterface.FailedPlayingMessage(currentSong, VoiceChannel, _musicStructure, e));
                    continue;
                }
                
                if (yStream == null)
                {
                    await TextMessageChannel.SendMessageAsync(
                        embed: TextInterface.FailedPlayingMessage(currentSong, VoiceChannel, _musicStructure,
                            new NullReferenceException("The Video was not found")));
                    continue;
                }

                var msg = await TextMessageChannel.SendMessageAsync(
                    embed: TextInterface.NowPlayingMessage(currentSong, VoiceChannel, _musicStructure));

                for (int i = 0; i < 5; i++)
                {
                    var status = await StreamCopy(yStream, await GetGuildStream());
                    if (status || currentSong.IsSkipped) break;
                    await Task.Delay(200).ConfigureAwait(false);
                }

                await _guildAudioStream.FlushAsync();

                if (currentSong.ShowSkipMessage)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Embed = TextInterface.SkipSongMessage(currentSong, VoiceChannel, _musicStructure);
                    });
                }
                else
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Embed = TextInterface.StoppedSongMessage(currentSong, VoiceChannel, _musicStructure);
                    });
                }
            }

            if (_guildAudioStream != null)
            {
                await _guildAudioStream.DisposeAsync();
                _guildAudioStream = null;
            }

            if (_guildAudioClient != null)
            {
                await _guildAudioClient.StopAsync();
                _guildAudioClient = null;
            }

            VoiceChannel = null;
            TextMessageChannel = null;

            _playbackSlim.Release();
        }

        private async Task<bool> StreamCopy(Stream src, Stream dest)
        {
            var buf = new byte[65536];
            while (_guildAudioClient.ConnectionState == ConnectionState.Connected)
            {
                try
                {
                    var len = await src.ReadAsync(buf, 0, buf.Length);

                    if (len == 0)
                    {
                        return true;
                    }

                    await dest.WriteAsync(buf, 0, len);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
