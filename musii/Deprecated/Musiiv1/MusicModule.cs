using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using musii.Utilities;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace musii.Deprecated.Musiiv1
{
    /// <summary>
    /// Deprecated, Kept in as reference source code
    /// </summary>
    [ObsoleteAttribute("This class is deprecated, please use MusicBotModule")]
    internal class MusicModule : ModuleBase<SocketCommandContext>
    {
        public static YoutubeClient client = new YoutubeClient();
        public static Dictionary<ulong, ConcurrentQueue<Video>> activePlaying = new Dictionary<ulong, ConcurrentQueue<Video>>();

        public static HashSet<ulong> activeGuilds = new HashSet<ulong>();
        public static HashSet<ulong> busyGuilds = new HashSet<ulong>();

        [Command("play",  RunMode = RunMode.Async), Alias("p", "pl", "listen", "yt", "youtube")] 
        public async Task Play(params string[] keywords)
        {
            var channel = Context.Channel;
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }
            PlayMusic(keywords, voiceChannel, Context);
        }

        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public async Task Skip()
        {
            var channel = Context.Channel;
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }

            if (activePlaying.ContainsKey(voiceChannel.Id))
            {
                bool k = true;
                while (k && activePlaying[voiceChannel.Id].Count > 0)
                {
                    k = !activePlaying[voiceChannel.Id].TryDequeue(out var l);
                    if (!k)
                    {
                        await channel.SendMessageAsync($"Removed ``{l.Title}`` from the queue.").ConfigureAwait(false);
                        break;
                    }
                }
            }
        }

        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public async Task Skip(int count)
        {
            var channel = Context.Channel;
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }

            int cnt = 0;
            for (int i = 0; i < count; i++)
            {
                if (activePlaying.ContainsKey(voiceChannel.Id))
                {
                    bool k = true;
                    while (k && activePlaying[voiceChannel.Id].Count > 0)
                    {
                        k = !activePlaying[voiceChannel.Id].TryDequeue(out var l);
                        if (!k)
                        {
                            cnt++;
                            break;
                        }
                    }
                }
            }
            await channel.SendMessageAsync($"Removed ``{cnt}`` videos from the queue.").ConfigureAwait(false);
        }

        [Command("queue",  RunMode = RunMode.Async), Alias("q")] 
        public async Task Queue()
        {
            var channel = Context.Channel;
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }

            if (activePlaying.ContainsKey(voiceChannel.Id))
            {
                int min = Math.Min(20, activePlaying[voiceChannel.Id].Count);
                StringBuilder sb = new StringBuilder();
                var l = activePlaying[voiceChannel.Id].Take(min);
                int cnt = 0;

                var eb = new EmbedBuilder {Title = $"Items In Queue"};
                eb.WithColor(Color.Blue);

                if (min == activePlaying[voiceChannel.Id].Count)
                {
                    eb.WithFooter("End of playlist.");
                }
                else
                {
                    eb.WithFooter("Next 20 items.");
                }

                foreach (var v in l)
                {
                    cnt++;
                    if (cnt == 1)
                    {
                        sb.Append($"**[{cnt}]: `{v.Title}`**\n");
                    }
                    else
                    {
                        sb.Append($"[{cnt}]: `{v.Title}`\n");
                    }
                }

                eb.WithDescription(sb.ToString());
                await channel.SendMessageAsync(null, false, eb.Build()).ConfigureAwait(false);
            }
            
        }

        [Command("leave",  RunMode = RunMode.Async), Alias("empty","clear","c")] 
        public async Task Leave()
        {
            var channel = Context.Channel;
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await channel.SendMessageAsync($"You must be in a voice channel to execute this command!").ConfigureAwait(false);
                return;
            }

            if (activePlaying.ContainsKey(voiceChannel.Id))
            {
                await channel.SendMessageAsync($"Removed ``{activePlaying[voiceChannel.Id].Count}`` videos from the queue.").ConfigureAwait(false);
                activePlaying[voiceChannel.Id].Clear();
            }
        }

        public static async Task PlayMusic(string[] keywords, IVoiceChannel voiceChannel, SocketCommandContext Context)
        {
            var channel = Context.Channel;

            if (!activePlaying.ContainsKey(voiceChannel.Id))
            {
                activePlaying[voiceChannel.Id] = new ConcurrentQueue<Video>();
            }

            bool alreadyPlaying = !activePlaying[voiceChannel.Id].IsEmpty;

            if (!alreadyPlaying && activeGuilds.Contains(Context.Guild.Id))
            {
                await channel.SendMessageAsync($"There can only be one active music channel!").ConfigureAwait(false);
                return;
            }

            if (busyGuilds.Contains(Context.Guild.Id))
            {
                await channel.SendMessageAsync($"Please wait a few seconds before running this command again!").ConfigureAwait(false);
                return;
            }
            
            Uri uriResult;
            bool result = Uri.TryCreate(keywords[0], UriKind.Absolute, out uriResult) 
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if(result && PlaylistId.TryParse(keywords[0]).HasValue)
            {
                busyGuilds.Add(Context.Guild.Id);
                var playlist = PlaylistId.TryParse(keywords[0]).Value;
                var videos = client.Playlists.GetVideosAsync(playlist.Value);
                int cnt = 0;
                int dup = 0;
                await foreach (var playlistVideo in videos)
                {
                    if (!activePlaying[voiceChannel.Id].Contains(playlistVideo, new VideoEqualityComparator()))
                    {
                        cnt++;
                        activePlaying[voiceChannel.Id].Enqueue(playlistVideo);
                    }
                    else
                    {
                        dup++;
                    }
                    if (cnt >= 1000)
                    {
                        await channel.SendMessageAsync($"Playlist exceeded 1000 videos! Only queuing the first 1000.").ConfigureAwait(false);
                        break;
                    }
                }

                if (dup != 0)
                {
                    await channel.SendMessageAsync($"Queued `{cnt}` videos, {dup} removed duplicates.").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendMessageAsync($"Queued `{cnt}` videos.").ConfigureAwait(false);
                }
                busyGuilds.Remove(Context.Guild.Id);

            } else if (result && VideoId.TryParse(keywords[0]).HasValue)
            {
                busyGuilds.Add(Context.Guild.Id);
                var id = VideoId.TryParse(keywords[0]).Value;
                try
                {
                    var vid = await client.Videos.GetAsync(id).ConfigureAwait(false);

                    if (!activePlaying[voiceChannel.Id].Contains(vid, new VideoEqualityComparator()))
                    {
                        if (activePlaying[voiceChannel.Id].Count > 0)
                        {
                            await channel.SendMessageAsync($"Queued `{vid.Title}`.").ConfigureAwait(false);
                        }
                        activePlaying[voiceChannel.Id].Enqueue(vid);
                    }
                    else
                    {
                        await channel.SendMessageAsync($"The queue already contains `{vid.Title}`.").ConfigureAwait(false);
                    }
                }
                catch
                {
                    await channel.SendMessageAsync($"There was an error trying to find that video.").ConfigureAwait(false);
                }
                busyGuilds.Remove(Context.Guild.Id);
            }
            else
            {
                busyGuilds.Add(Context.Guild.Id);
                var videos = client.Search.GetVideosAsync(string.Join(' ', keywords));
                try
                {
                    var vid = await videos.FirstAsync().ConfigureAwait(false);
                    if (!activePlaying[voiceChannel.Id].Contains(vid, new VideoEqualityComparator()))
                    {
                        if (activePlaying[voiceChannel.Id].Count > 0)
                        {
                            await channel.SendMessageAsync($"Queued `{vid.Title}`.").ConfigureAwait(false);
                        }
                        activePlaying[voiceChannel.Id].Enqueue(vid);
                    }
                    else
                    {
                        await channel.SendMessageAsync($"The queue already contains `{vid.Title}`.").ConfigureAwait(false);
                    }
                }
                catch
                {
                    await channel.SendMessageAsync($"There was an error trying to find that video.").ConfigureAwait(false);
                }
                busyGuilds.Remove(Context.Guild.Id);
            }

            if (!alreadyPlaying)
            {
                activeGuilds.Add(Context.Guild.Id);

                try
                {
                    await voiceChannel.DisconnectAsync().ConfigureAwait(false);
                    await Task.Delay(500).ConfigureAwait(false);

                    var audioClient = await voiceChannel.ConnectAsync().ConfigureAwait(false);
                    var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

                    while (activePlaying[voiceChannel.Id].Count > 0)
                    {
                        Video v = activePlaying[voiceChannel.Id].First();
                        StreamManifest manifest;
                        try
                        {
                            manifest = await client.Videos.Streams.GetManifestAsync(v.Id).ConfigureAwait(false);
                        }
                        catch
                        {
                            await Task.Delay(500).ConfigureAwait(false);
                            try
                            {
                                manifest = await client.Videos.Streams.GetManifestAsync(v.Id).ConfigureAwait(false);
                            }
                            catch(Exception e)
                            {
                                var ebe = new EmbedBuilder();
                                ebe.Title = $"Failed to play `{v.Title}`.";
                                ebe.WithColor(Color.Orange);
                                ebe.WithDescription(
                                    $"Failed playing video `{v.Title}` [{MessageSender.TimeSpanFormat(v.Duration)}] in `{voiceChannel.Name}`.");
                                ebe.WithFooter("Video Link: " + v.Url);
                                ebe.WithThumbnailUrl(v.Thumbnails.HighResUrl);

                                await channel.SendMessageAsync(embed: ebe.Build()).ConfigureAwait(false);

                                e.StackTrace.Log();

                                // spaghetti code lol
                                goto SKIPPLAY;
                            }
                        }


                        var eb = new EmbedBuilder {Title = $"Now Playing `{v.Title}`."};
                        eb.WithColor(Color.Blue);
                        eb.WithDescription(
                            $"Playing video `{v.Title}` [{MessageSender.TimeSpanFormat(v.Duration)}] in `{voiceChannel.Name}`.");
                        eb.WithFooter("Video Link: " + v.Url);
                        eb.WithThumbnailUrl(v.Thumbnails.HighResUrl);

                        var msg = await channel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);

                        var active = new Ref<bool>(false);

                        // Start Closed Captions
                        Captioner(active, v, Context);

                        // Play video
                        var streamManifest = manifest.GetAudioOnly().WithHighestBitrate();

                        var mpeg = CreateFFMPEG(streamManifest.Url);


                        using (var youtube = mpeg.StandardOutput.BaseStream)
                        using (var youtubeBuffer = new BufferedStream(youtube, 65536))
                        {
                            long count = 0;
                            var buf = new byte[8192];

                            while (true)
                            {
                                count++;
                                try
                                {
                                    var len = await youtubeBuffer.ReadAsync(buf, 0, 8192).ConfigureAwait(false);
                                    await discord.WriteAsync(buf, 0, len).ConfigureAwait(false);
                                    active.obj = true;
                                    if (len == 0) break;
                                    if (count % 1000 == 0)
                                    {
                                        var temp = await voiceChannel.GetUsersAsync().FlattenAsync()
                                            .ConfigureAwait(false);
                                        if (temp.Count() <= 1)
                                        {
                                            break;
                                        }
                                    }

                                    if (count % 10 == 0)
                                    {
                                        if ((activePlaying[voiceChannel.Id].IsEmpty ||
                                             v.Id != activePlaying[voiceChannel.Id].First().Id))
                                        {
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    e.StackTrace.Log();
                                    await voiceChannel.DisconnectAsync().ConfigureAwait(false);
                                    audioClient = await voiceChannel.ConnectAsync().ConfigureAwait(false);
                                    await discord.DisposeAsync().ConfigureAwait(false);
                                    discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
                                }
                            }

                            active.obj = false;
                        }

                        eb.WithColor(Color.Red);
                        eb.WithTitle($"Stopped Playing `{v.Title}`.");
                        eb.WithDescription(
                            $"Stopped playing video `{v.Title}` [{MessageSender.TimeSpanFormat(v.Duration)}] in `{voiceChannel.Name}`.");
                        eb.WithFooter("Video Link: " + v.Url);
                        await msg.ModifyAsync(x => { x.Embed = eb.Build(); }).ConfigureAwait(false);

                        SKIPPLAY:

                        if (activePlaying[voiceChannel.Id].IsEmpty) break;
                        bool k = true;
                        while (k)
                        {
                            if (v.Id == activePlaying[voiceChannel.Id].First().Id)
                            {
                                k = !activePlaying[voiceChannel.Id].TryDequeue(out _);
                            }
                            else
                            {
                                break;
                            }
                        }

                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    await discord.DisposeAsync().ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    e.StackTrace.Log();
                    await channel.SendMessageAsync($"An unknown error occurred! Please contact the developer! "+e.StackTrace).ConfigureAwait(false);
                }

                activeGuilds.Remove(Context.Guild.Id);
                await voiceChannel.DisconnectAsync().ConfigureAwait(false);
            }
        }

        public static async Task Captioner(Ref<bool> active, Video v, SocketCommandContext Context)
        {
            var captionTracks =
                (await client.Videos.ClosedCaptions.GetManifestAsync(v.Id).ConfigureAwait(false)).Tracks;
            ClosedCaptionTrackInfo captionMeta = null;
            foreach (var track in captionTracks)
            {
                if (track.Language.Code.StartsWith("en"))
                {
                    captionMeta = track;
                    break;
                }
            }
            if (captionMeta == null)
            {
                return;
            }
            var captions = await client.Videos.ClosedCaptions.GetAsync(captionMeta).ConfigureAwait(false);

            var eb = new EmbedBuilder();
            eb.Title = $"Closed Captions [`{captionMeta.Language.Name}`]";
            eb.WithFooter(v.Title , v.Thumbnails.LowResUrl);
            eb.WithColor(Color.Red);

            var msg = await Context.Channel.SendMessageAsync(null, false, eb.Build()).ConfigureAwait(false);

            while (!active.obj)
            {
                if (active.obj)
                {
                    break;
                }
            }

            DateTime start = DateTime.Now;

            foreach (var cap in captions.Captions)
            {
                if (cap != null)
                {
                    var st = cap.Offset;

                    var lines = SplitToLines(cap.Text, 1980);

                    if (DateTime.Now - start < st)
                    {
                        await Task.Delay(st - (DateTime.Now - start)).ConfigureAwait(false);
                    }

                    foreach (string ss in lines)
                    {
                        var gmst = await Context.Channel.GetMessageAsync(msg.Id).ConfigureAwait(false);
                        if (gmst == null)
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                            return;
                        }

                        eb.WithDescription($"**[{MessageSender.TimeSpanFormat(st)}]** `" + ss + "`");
                        await msg.ModifyAsync(x => { x.Embed = eb.Build(); }).ConfigureAwait(false);

                        if (!active.obj)
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                            return;
                        }
                        
                    }
                }
            }
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        private static List<string> SplitToLines(string stringToSplit, int maxLineLength)
        {
            List<string> l = new List<string>();
            string[] words = stringToSplit.Split(' ');
            StringBuilder line = new StringBuilder();
            foreach (string word in words)
            {
                if (word.Length + line.Length <= maxLineLength)
                {
                    line.Append(word + " ");
                }
                else
                {
                    if (line.Length > 0)
                    {
                        l.Add(line.ToString().Trim());
                        line.Clear();
                    }
                    string overflow = word;
                    while (overflow.Length > maxLineLength)
                    {
                        l.Add(overflow.Substring(0, maxLineLength));
                        overflow = overflow.Substring(maxLineLength);
                    }
                    line.Append(overflow + " ");
                }
            }
            l.Add(line.ToString().Trim());
            return l;
        }

        private static Process CreateFFMPEG(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ProcessStartInfo proc = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 2 -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                };
                return Process.Start(proc);
            }
            else
            {
                ProcessStartInfo proc = new ProcessStartInfo
                {
                    FileName = "ffmpeg.windows",
                    Arguments = $"-hide_banner -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 2 -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                };
                return Process.Start(proc);
            }
        }

    }
}
