using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using Discord.Rest;
using Discord.WebSocket;
using musii.Utilities;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace musii.Music
{
    class GuildMusic
    {
        public bool IsActive;

        public bool IsPlaying;

        public ConcurrentQueue<MusicRequest> VideoQueue = new ConcurrentQueue<MusicRequest>();

        public TimeSpan QueueLength = TimeSpan.Zero;

        public MusicRequest ActiveVideo;

        public IVoiceChannel ActiveVoiceChannel;

        public IAudioClient ActiveAudioClient;

        public IGuild Guild;

        public YoutubeClient Client = new YoutubeClient();

        private CancellationToken _skipToken;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public delegate void DeactivateDelegate(IGuild guild);

        public DeactivateDelegate MusicStoppedDelegate;

        public async Task<MusicRequest> GetNextRequestAsync()
        {
            if (VideoQueue.IsEmpty) return null;

            return await Task.Run(() =>
            {
                while (!VideoQueue.IsEmpty)
                {
                    var success = VideoQueue.TryDequeue(out var req);
                    if (success) return req;
                }

                return null;
            }, _skipToken).ConfigureAwait(false);
        }

        public async Task<RestUserMessage> NowPlayingAsync(MusicRequest request,
            ISocketMessageChannel activeTextChannel)
        {
            var v = request.RequestedVideo;

            var user = await Guild.GetUserAsync(request.RequestedBy).ConfigureAwait(false);

            var eb = new EmbedBuilder {Title = $"Now Playing `{v.Title}`."};
            eb.WithColor(Color.Blue);
            eb.WithDescription(
                $"Playing video `{v.Title}` [`{MessageSender.TimeSpanFormat(v.Duration)}`] in `{ActiveVoiceChannel.Name}`.");
            eb.WithFooter(v.Url + " Requested By: " + user.Username + "#" + user.Discriminator);
            eb.WithThumbnailUrl(v.Thumbnails.HighResUrl);

            return await activeTextChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
        }

        public async Task<Task> FailedPlayingAsync(MusicRequest request, RestUserMessage msg,
            ISocketMessageChannel activeTextChannel)
        {
            var v = request.RequestedVideo;

            var user = await Guild.GetUserAsync(request.RequestedBy).ConfigureAwait(false);

            var eb = new EmbedBuilder {Title = $"Failed to play `{v.Title}`."};
            eb.WithColor(Color.Orange);
            eb.WithDescription(
                $"Failed playing video `{v.Title}` [`{MessageSender.TimeSpanFormat(v.Duration)}`] in `{ActiveVoiceChannel.Name}`.");
            eb.WithFooter(v.Url + " Requested By: " + user.Username + user.Discriminator);
            eb.WithThumbnailUrl(v.Thumbnails.HighResUrl);

            return msg.ModifyAsync(x => { x.Embed = eb.Build(); });
        }

        public async Task QueueMusicAsync(ISocketMessageChannel ActiveTextChannel, params MusicRequest[] requests)
        {
            if (requests.Length == 1)
            {
                VideoQueue.Enqueue(requests[0]);
                await QueuedMessageAsync(requests[0], ActiveTextChannel).ConfigureAwait(false);

                QueueLength += requests[0].RequestedVideo.Duration;
            }
            else
            {
                foreach (var req in requests)
                {
                    VideoQueue.Enqueue(req);
                    QueueLength += req.RequestedVideo.Duration;
                }

                var eb = new EmbedBuilder();
                eb.WithColor(Color.Green);
                eb.WithTitle($"Queued `{requests.Length}` videos.");
                eb.WithDescription(
                    $"The queue now has `{VideoQueue.Count}` videos [`{MessageSender.TimeSpanFormat(QueueLength)}`].");
                await ActiveTextChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            }

            TaskUtils.Forget(() => PlayAsync(ActiveTextChannel));
        }

        public async Task SkipMusicAsync(int count, ISocketMessageChannel activeTextChannel)
        {
            if (count == 1)
            {
                _isSkipped = true;
                _cancellationTokenSource.Cancel();
            }
            else
            {
                int cnt = 1;
                while (cnt < count && !VideoQueue.IsEmpty)
                {
                    var k = await GetNextRequestAsync().ConfigureAwait(false);
                    QueueLength -= k.RequestedVideo.Duration;
                    cnt++;
                }

                _cancellationTokenSource.Cancel();

                var eb = new EmbedBuilder();
                eb.WithColor(Color.Green);
                eb.WithTitle($"Skipped `{cnt}` videos.");
                eb.WithDescription(
                    VideoQueue.IsEmpty
                        ? $"The playlist is now `empty`."
                        : $"There are `{VideoQueue.Count}` remaining videos.");
                await activeTextChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            }
        }

        public Task SkippedPlayingAsync(MusicRequest request, RestUserMessage msg,
            ISocketMessageChannel ActiveTextChannel)
        {
            var v = request.RequestedVideo;

            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkerGrey);
            eb.WithTitle($"Skipped Playing `{v.Title}`.");
            eb.WithDescription(
                $"Skipped playing video `{v.Title}` [`{MessageSender.TimeSpanFormat(v.Duration)}`] in `{ActiveTextChannel.Name}`.");
            eb.WithFooter(v.Url);
            return msg.ModifyAsync(x => { x.Embed = eb.Build(); });
        }

        public Task StoppedPlayingAsync(MusicRequest request, RestUserMessage msg,
            ISocketMessageChannel ActiveTextChannel)
        {
            var v = request.RequestedVideo;

            var eb = new EmbedBuilder();
            eb.WithColor(Color.Red);
            eb.WithTitle($"Stopped Playing `{v.Title}`.");
            eb.WithDescription(
                $"Stopped playing video `{v.Title}` [`{MessageSender.TimeSpanFormat(v.Duration)}`] in `{ActiveTextChannel.Name}`.");
            eb.WithFooter(v.Url);
            return msg.ModifyAsync(x => { x.Embed = eb.Build(); });
        }

        public Task QueuedMessageAsync(MusicRequest request, ISocketMessageChannel ActiveTextChannel)
        {
            if (ActiveVideo != null)
            {
                var v = request.RequestedVideo;

                var eb = new EmbedBuilder();
                eb.WithColor(Color.Blue);
                eb.WithTitle($"Queued video `{v.Title}`.");
                eb.WithDescription(
                    $"`{v.Title}` [`{MessageSender.TimeSpanFormat(v.Duration)}`.]\n" +
                    $"The queue now has `{VideoQueue.Count}` videos [`{MessageSender.TimeSpanFormat(QueueLength)}`].");
                eb.WithFooter(v.Url);

                return ActiveTextChannel.SendMessageAsync(embed: eb.Build());
            }

            return Task.CompletedTask;
        }

        public Task GetQueueAsync(ISocketMessageChannel ActiveTextChannel)
        {
            int min = Math.Min(20, VideoQueue.Count);
            StringBuilder sb = new StringBuilder();
            var l = VideoQueue.Take(min);
            int cnt = 0;

            var eb = new EmbedBuilder {Title = $"Items In Queue [`{VideoQueue.Count} - {MessageSender.TimeSpanFormat(QueueLength)}`]"};
            eb.WithColor(Color.Blue);

            if (min == VideoQueue.Count)
            {
                eb.WithFooter($"End of playlist.");
            }
            else
            {
                eb.WithFooter($"Next 20 items.");
            }

            if (ActiveVideo == null)
            {
                sb.Append($"**Not Playing.**\n");
            }
            else
            {
                sb.Append($"**Now Playing:** `{ActiveVideo.RequestedVideo.Title}`\n");
                TimeSpan playedTime = DateTime.Now - ActiveVideo.StartedPlayingTime;
                TimeSpan length = ActiveVideo.RequestedVideo.Duration;

                sb.Append($"**{MessageSender.TimeSpanFormat(playedTime)} / {MessageSender.TimeSpanFormat(length)}**\n");
            }

            sb.Append($"\n");

            sb.Append($"**Next up:**\n");

            foreach (var v in l)
            {
                cnt++;
                sb.Append($"[{cnt}]: `{v.RequestedVideo.Title}`\n");
            }

            eb.WithDescription(sb.ToString());
            return ActiveTextChannel.SendMessageAsync(null, false, eb.Build());
        }

        private bool _isSkipped;

        /// <summary>
        /// Called when bot leaves channel
        /// </summary>
        /// 
        public void Stop()
        {
            if (IsActive)
            {
                IsActive = false;
                QueueLength = TimeSpan.Zero;
                ActiveVideo = null;
                VideoQueue.Clear();
                MusicStoppedDelegate.Invoke(Guild);
                _cancellationTokenSource.Cancel();
                ActiveVoiceChannel.DisconnectAsync();
            }
        }
        
        public async Task PlayAsync(ISocketMessageChannel ActiveTextChannel, AudioOutStream dStream = null)
        {
            try
            {
                bool playSuccess = true;

                if (IsPlaying || !IsActive)
                {
                    if (dStream != null) await dStream.DisposeAsync().ConfigureAwait(false);
                    return;
                }

                IsPlaying = true;

                _cancellationTokenSource = new CancellationTokenSource();

                _skipToken = _cancellationTokenSource.Token;

                var next = await GetNextRequestAsync().ConfigureAwait(false);
                if (next == null)
                {
                    Stop();
                    return;
                }

                ActiveVideo = next;

                ActiveVideo.StartedPlayingTime = DateTime.Now;

                StreamManifest manifest;

                IStreamInfo streamInfo = null;

                // Display Now Playing message
                var msg = await NowPlayingAsync(ActiveVideo, ActiveTextChannel).ConfigureAwait(false);

                try
                {
                    manifest = await Client.Videos.Streams.GetManifestAsync(ActiveVideo.VideoId).ConfigureAwait(false);
                    streamInfo = manifest.GetAudioOnly().WithHighestBitrate();
                }
                catch
                {
                    await Task.Delay(500, _skipToken).ConfigureAwait(false);
                    try
                    {
                        manifest = await Client.Videos.Streams.GetManifestAsync(ActiveVideo.VideoId)
                            .ConfigureAwait(false);
                        streamInfo = manifest.GetAudioOnly().WithHighestBitrate();
                    }
                    catch
                    {
                        await FailedPlayingAsync(ActiveVideo, msg, ActiveTextChannel).ConfigureAwait(false);
                        playSuccess = false;
                    }
                }

                if (streamInfo == null && playSuccess)
                {
                    await FailedPlayingAsync(ActiveVideo, msg, ActiveTextChannel).ConfigureAwait(false);
                    playSuccess = false;
                }

                if (playSuccess)
                {
                    // Start FFMPEG
                    var mpeg = Ffmpeg.CreateFfmpeg(streamInfo.Url);

                    Stream youtube = null;

                    if (dStream == null)
                    {
                        if (ActiveAudioClient == null || ActiveAudioClient.ConnectionState != ConnectionState.Connected)
                        {
                            ActiveAudioClient = await ActiveVoiceChannel.ConnectAsync(true).ConfigureAwait(false);
                        }
                        dStream = ActiveAudioClient.CreatePCMStream(AudioApplication.Mixed);
                    }

                    youtube = mpeg.StandardOutput.BaseStream;

                    // Start Caption Service
                    TaskUtils.Forget(() => CaptionService.CaptionAsync(_skipToken, ActiveVideo, Client, ActiveTextChannel));

                    // Check if bot is playing music to the wall
                    TaskUtils.Recur(async () =>
                    {
                        var temp = await ActiveVoiceChannel.GetUsersAsync().FlattenAsync()
                            .ConfigureAwait(false);
                        if (temp.Count() <= 1)
                        {
                            Stop();
                        }
                    }, TimeSpan.FromSeconds(20), _skipToken);

                    var buf = new byte[65536];

                    int retries = 0;

                    while (!_skipToken.IsCancellationRequested)
                    {
                        try
                        {
                            var len = await youtube.ReadAsync(buf, 0, 65536, _skipToken).ConfigureAwait(false);

                            if (len == 0) _cancellationTokenSource.Cancel();

                            await dStream.WriteAsync(buf, 0, len, _skipToken).ConfigureAwait(false);

                            retries = 0;
                        }
                        catch (Exception e)
                        {
                            if (!(e is OperationCanceledException))
                            {
                                try
                                {
                                    dStream.Dispose();

                                    await ActiveVoiceChannel.DisconnectAsync().ConfigureAwait(false);

                                    await ActiveAudioClient.StopAsync().ConfigureAwait(false);

                                    await Task.Delay(500, _skipToken).ConfigureAwait(false);

                                    ActiveAudioClient = await ActiveVoiceChannel.ConnectAsync(true).ConfigureAwait(false);

                                    dStream = ActiveAudioClient.CreatePCMStream(AudioApplication.Mixed);

                                    retries++;

                                    if (retries > 100)
                                    {
                                        _cancellationTokenSource.Cancel();
                                    }
                                }
                                catch
                                {
                                    // No Internet Connectivity? Channel Deleted?
                                    _cancellationTokenSource.Cancel();
                                }
                            }
                        }
                    }

                    await Task.Delay(500).ConfigureAwait(false);

                    try
                    {
                        dStream.Flush();
                    }
                    catch
                    {
                    }

                    try
                    {
                        await youtube.DisposeAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }

                    mpeg.Kill();
                }

                if (!_skipToken.IsCancellationRequested) _cancellationTokenSource.Cancel();

                if (playSuccess)
                {
                    if (_isSkipped)
                    {
                        await SkippedPlayingAsync(ActiveVideo, msg, ActiveTextChannel).ConfigureAwait(false);
                    }
                    else
                    {
                        await StoppedPlayingAsync(ActiveVideo, msg, ActiveTextChannel).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }

            try
            {
                QueueLength -= ActiveVideo.RequestedVideo.Duration;
            }
            catch
            {

            }

            ActiveVideo = null;
            IsPlaying = false;

            await PlayAsync(ActiveTextChannel, dStream).ConfigureAwait(false);
        }
    }
}
