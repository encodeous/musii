using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using musii.Utilities;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace musii.Deprecated.Musiiv2
{
    public class MusicPlayer
    {
        private Dictionary<ulong, GuildMusic> _guilds = new Dictionary<ulong, GuildMusic>();

        private void MusicStoppedCallback(IGuild guild)
        {
            _guilds.Remove(guild.Id);
        }

        private bool IsUrl(string s)
        {
            return Uri.TryCreate(s, UriKind.Absolute, out var uriResult) 
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public void Stop()
        {
            foreach (var g in _guilds.Values)
            {
                if (g.IsActive)
                {
                    g.Stop();
                }
            }
        }

        public async Task Play(SocketCommandContext context, string[] keywords)
        {
            var channel = context.Channel;
            var voiceChannel = (context.User as IGuildUser)?.VoiceChannel;

            if (!_guilds.ContainsKey(context.Guild.Id))
            {
                if (voiceChannel == null)
                {
                    await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                    return;
                }

                GuildMusic music = new GuildMusic
                {
                    ActiveVoiceChannel = voiceChannel, Guild = context.Guild, IsActive = true
                };

                music.MusicStoppedDelegate += MusicStoppedCallback;

                _guilds[context.Guild.Id] = music;
            }

            var g = _guilds[context.Guild.Id];

            bool result = IsUrl(keywords[0]);
            if(result && PlaylistId.TryParse(keywords[0]).HasValue)
            {
                var playlist = PlaylistId.TryParse(keywords[0]).Value;
                var videos = g.Client.Playlists.GetVideosAsync(playlist.Value);
                int cnt = 0;

                List<MusicRequest> requests = new List<MusicRequest>();

                await foreach (var playlistVideo in videos)
                {
                    requests.Add(new MusicRequest(){RequestedBy = context.User.Id, RequestedVideo = playlistVideo, VideoId = playlistVideo.Id});
                    cnt++;
                    if (cnt >= 100)
                    {
                        break;
                    }
                }

                await g.QueueMusicAsync(context.Channel, requests.ToArray()).ConfigureAwait(false);


            } else if (result && VideoId.TryParse(keywords[0]).HasValue)
            {
                var id = VideoId.TryParse(keywords[0]).Value;
                try
                {
                    var vid = await g.Client.Videos.GetAsync(id).ConfigureAwait(false);

                    await g.QueueMusicAsync(context.Channel, new MusicRequest(){RequestedBy = context.User.Id, RequestedVideo = vid, VideoId = vid.Id}).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync($"There was an error trying to find that video.").ConfigureAwait(false);
                }
            }
            else
            {
                var videos = g.Client.Search.GetVideosAsync(string.Join(' ', keywords));
                try
                {
                    var vid = await videos.FirstAsync().ConfigureAwait(false);

                    await g.QueueMusicAsync(context.Channel, new MusicRequest(){RequestedBy = context.User.Id, RequestedVideo = vid, VideoId = vid.Id}).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync($"There was an error trying to find that video.").ConfigureAwait(false);
                }
            }

        }

        public async Task Clear(SocketCommandContext context)
        {
            if (_guilds.ContainsKey(context.Guild.Id))
            {
                var g = _guilds[context.Guild.Id];
                if (g.IsActive)
                {
                    await g.SkipMusicAsync(g.VideoQueue.Count + 2, context.Channel).ConfigureAwait(false);
                }
            }
        }

        public async Task Skip(SocketCommandContext context, int cnt)
        {
            if (_guilds.ContainsKey(context.Guild.Id))
            {
                var g = _guilds[context.Guild.Id];
                if (g.IsActive)
                {
                    await g.SkipMusicAsync(cnt, context.Channel).ConfigureAwait(false);
                }
            }
        }

        public async Task ShowQueue(SocketCommandContext context)
        {
            if (_guilds.ContainsKey(context.Guild.Id))
            {
                var g = _guilds[context.Guild.Id];
                if (g.IsActive)
                {
                    await g.GetQueueAsync(context.Channel).ConfigureAwait(true);
                }
            }
        }
    }
}
