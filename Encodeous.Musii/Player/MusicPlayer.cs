using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii.Player
{
    /// <summary>
    /// A player represents a playing session. Players will execute a PlayerState
    /// </summary>
    public class MusicPlayer : IDisposable
    {
        #region Properties and Fields

        public SearchService Searcher;
        public bool IsInitialized { get; private set; }
        public bool IsPlaying { get; private set; }

        public ScopeData Data;
        private bool _isDisposed = false;
        private ILogger<MusicPlayer> _log;
        private DiscordClient _client;
        private DiscordMessage _queueMessage = null;

        #endregion

        public MusicPlayer(ILogger<MusicPlayer> log, SearchService searcher, ScopeData data, DiscordClient client)
        {
            _log = log;
            Searcher = searcher;
            Data = data;
            _client = client;
        }

        #region Initialization

        public async Task ConnectPlayer()
        {
            Data.LavalinkNode = await _client.GetLavalink().GetIdealNodeConnection().ConnectAsync(Data.VoiceChannel);
        }

        /// <summary>
        /// Attach a state to the player (Allows for pause/resume, channel moving, auto-reconnections, session saving... etc)
        /// </summary>
        /// <param name="state"></param>
        public void InitializeState(PlayerState state)
        {
            if (Data.State is not null)
            {
                Data.State.IsPlaying = false;
            }
            IsInitialized = true;
            Data.State = state;
        }
        
        /// <summary>
        /// Starts playing music, must already have a song!
        /// </summary>
        public async Task StartPlaying()
        {
            if (IsPlaying) return;
            if (Data.State is null || (Data.State.CurrentTrack is null && !Data.State.Tracks.Any())) throw new Exception("Music player started playing without a state!");
            Data.State.IsPlaying = true;
            IsPlaying = true;
            Data.LavalinkNode.DiscordWebSocketClosed += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await SaveSession();
                if (args.Code == 4014)
                {
                    await Data.TextChannel.SendMessageAsync(Messages.GenericError(
                        "Disconnected by Moderator", "The bot will leave", ""));
                    _log.LogDebug($"Bot voice websocket closed {args.Reason} with code {args.Code}");
                    Dispose();
                    return;
                }
                Dispose();
            };
            Data.LavalinkNode.PlaybackFinished += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                if (args.Reason == TrackEndReason.Finished)
                {
                    if (await MoveNextAsync())
                    {
                        await LavalinkPlay();
                    }
                }
                if (args.Reason == TrackEndReason.LoadFailed)
                {
                    await Data.TextChannel.SendMessageAsync(Messages.GenericError("Track load failed", 
                        $"The track `{args.Track.Title}` is not able to be played!", ""));
                    if (await MoveNextAsync())
                    {
                        await LavalinkPlay();
                    }
                }
            };
            Data.LavalinkNode.TrackException += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await Task.Delay(1000);
                await LavalinkPlay();
                _log.LogError($"Playback encountered error {args.Error} in channel {args.Player.Channel}");
            };
            Data.LavalinkNode.TrackStuck += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await Task.Delay(1000);
                await LavalinkPlay();
                _log.LogDebug($"Track stuck in channel {args.Player.Channel}");
            };
            Data.LavalinkNode.PlayerUpdated += (sender, args) =>
            {
                Data.State.CurrentTrack.SetPos((long) args.Position.TotalMilliseconds);
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
                return Task.CompletedTask;
            };
            if(Data.State.CurrentTrack is null) await MoveNextAsync();
            await LavalinkPlay();
            if (Data.State.IsPaused) await Data.LavalinkNode.PauseAsync();
            await SetVolume(Data.State.Volume);
        }
        
        public async Task LavalinkPlay()
        {
            await Data.LavalinkNode.PlayPartialAsync(Data.State.CurrentTrack,
                Data.State.CurrentTrack.Position, Data.State.CurrentTrack.Length);
        }

        #endregion

        #region Playback Control

        public async Task<bool> TogglePause()
        {
            if (!Data.State.IsPaused)
            {
                await Data.LavalinkNode.PauseAsync();
            }
            else
            {
                await Data.LavalinkNode.ResumeAsync();
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
            }

            Data.State.IsPaused = !Data.State.IsPaused;
            return Data.State.IsPaused;
        }
        
        public async Task ShuffleAsync()
        {
            await Data.State.StateLock.WaitAsync();
            try
            {
                Data.State.Tracks.Shuffle();
            }
            finally
            {
                Data.State.StateLock.Release();
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
            }
        }

        public async Task SetVolume(int vol)
        {
            await Data.State.StateLock.WaitAsync();
            try
            {
                Data.State.Volume = vol;
                await Data.LavalinkNode.SetVolumeAsync(vol);
            }
            finally
            {
                Data.State.StateLock.Release();
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
            }
        }
        
        public async Task<bool> MoveNextAsync()
        {
            await Data.State.StateLock.WaitAsync();
            try
            {
                if (!Data.State.Tracks.Any())
                {
                    await PlaylistEndedAsync();
                    Dispose();
                    return false;
                }
                var cur = Data.State.CurrentTrack;
                // Fetch track
                Data.State.CurrentTrack = await Data.State.Tracks.First().GetTrack(Data.LavalinkNode);
                Data.State.Tracks.RemoveAt(0);
                if (Data.State.IsLooped && cur is not null)
                {
                    cur.SetPos(0);
                    Data.State.Tracks.Add(new YoutubeSource(cur));
                }
            }
            finally
            {
                Data.State.StateLock.Release();
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
            }

            return true;
        }
        
        public async Task AddTracks(IMusicSource[] tracks, CommandContext ctx = null)
        {
            if (tracks.Length == 1)
            {
                if(ctx != null) await ctx.RespondAsync(Data.AddedTrackMessage(await tracks[0].GetTrack(Data.LavalinkNode)));
                else await Data.TextChannel.SendMessageAsync(Data.AddedTrackMessage(await tracks[0].GetTrack(Data.LavalinkNode)));
            }
            else
            {
                if(ctx != null) await ctx.RespondAsync(Data.AddedTracksMessage(tracks.Length));
                else await Data.TextChannel.SendMessageAsync(Data.AddedTracksMessage(tracks.Length));
            }
            await Data.State.StateLock.WaitAsync();
            try
            {
                Data.State.Tracks.AddRange(tracks);
            }
            finally
            {
                Data.State.StateLock.Release();
                Data.QueueUpdate.Set();
                Data.QueueUpdate.Reset();
            }
        }
        
        public async Task SendQueueMessage()
        {
            int curpg = 0;
            var msg = await Data.TextChannel.SendMessageAsync(BuildQueueEmbed(0, await GetQueue()));
            _queueMessage = msg;
            Task.Run(async () =>
            {
                var l = DiscordEmoji.FromName(_client, ":arrow_left:");
                var r = DiscordEmoji.FromName(_client, ":arrow_right:");
                int pPages = -1;
                Task<ReadOnlyCollection<Reaction>> pcol = null;
                Task qcol = null;
                while (!_isDisposed && msg == _queueMessage)
                {
                    var cq = await GetQueue();
                    await msg.ModifyAsync(async x => x.Embed = BuildQueueEmbed(curpg, cq));
                    int tpages = (int) Math.Ceiling(cq.Count / 20.0);
                    tpages = Math.Max(tpages, 1);
                    if (pPages != curpg)
                    {
                        pPages = curpg;
                        await msg.DeleteAllReactionsAsync();
                        if (curpg != 0)
                        {
                            await msg.CreateReactionAsync(l);
                        }
                        if (curpg != tpages - 1)
                        {
                            await msg.CreateReactionAsync(r);
                        }
                    }

                    Task updt = null;
                    if (qcol != null)
                    {
                        updt = qcol;
                    }
                    else
                    {
                        updt = Data.QueueUpdate.WaitAsync();
                    }
                    Task<ReadOnlyCollection<Reaction>> react = null;
                    if (pcol != null)
                    {
                        react = pcol;
                    }
                    else
                    {
                        react = msg.CollectReactionsAsync();
                    }
                    var tsk = await Task.WhenAny(updt, react);
                    if (updt.IsCompleted)
                    {
                        qcol = null;
                        pcol = react;
                    }
                    else
                    {
                        if (react.Result.Any(x => x.Emoji == l))
                        {
                            curpg--;
                            curpg = Math.Max(0, curpg);
                        }
                        if (react.Result.Any(x => x.Emoji == r))
                        {
                            curpg++;
                            curpg = Math.Min(tpages-1, curpg);
                        }
                        pcol = null;
                        qcol = updt;
                    }
                }
            });
        }
        
        private DiscordEmbedBuilder BuildQueueEmbed(int page, List<IMusicSource> q)
        {
            // 20 items per pag
            int tpages = (int) Math.Ceiling(q.Count / 20.0);
            tpages = Math.Max(tpages, 1);
            var sel = q.GetRange(page * 20, Math.Min(20, q.Count));
            var builder = new DiscordEmbedBuilder()
                .WithTitle($"Queue for {Data.VoiceChannel.Name}")
                .WithFooter($"Page {page+1}/{tpages}");
            if (sel.Count > 1)
            {
                builder.AddField($"In Queue {q.Count - 1}",
                    string.Join("\n", sel.GetRange(1, 19)
                        .Select(x => $"`{x.GetTrackName()}`")));
            }

            if (sel.Count >= 1)
            {
                var selTrack = Data.State.CurrentTrack;
                builder.AddField("Now Playing",
                    $"`{selTrack.Title}`\n{Utils.GetProgress(selTrack.Position / selTrack.Length)}\n" +
                    $"**{selTrack.Position.MusiiFormat()} / {selTrack.Length.MusiiFormat()}**");
            }
            else
            {
                builder.WithDescription("Queue is Empty.");
            }

            string footer = $"Playing in channel {Data.VoiceChannel.Name}";
            if (Data.State.IsLooped)
            {
                footer += " - LOOPED";
            }

            if (Data.State.IsPaused)
            {
                footer += " - PAUSED";
            }

            footer += $"\nPlaylist Id: {Data.State.StateId}";
            builder.WithFooter(footer);
            return builder;
        }
        
        public async Task<List<IMusicSource>> GetQueue()
        {
            var lst = new List<IMusicSource>();
            await Data.State.StateLock.WaitAsync();
            try
            {
                if (Data.State.CurrentTrack is not null)
                {
                    lst.Add(new YoutubeSource(Data.State.CurrentTrack));
                }
                lst.AddRange(Data.State.Tracks);
            }
            finally
            {
                Data.State.StateLock.Release();
            }

            return lst;
        }

        #endregion

        #region Session Management

        public async Task Detach()
        {
            IsPlaying = false;
            IsInitialized = false;
            Data.State.IsPlaying = false;
            try
            {
                await Data.LavalinkNode?.StopAsync();
            }
            catch
            {
                    
            }
            try
            {
                await Data.LavalinkNode?.DisconnectAsync();
            }
            catch
            {
                    
            }
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                try
                {
                    Data.DeletePlayerCallback.Invoke().Wait();
                }
                catch
                {
                    
                }
                Detach().GetAwaiter().GetResult();
            }
        }
        
        private async Task PlaylistEndedAsync()
        {
            await Data.TextChannel.SendMessageAsync(Data.PlaylistEmptyMessage());
        }
        
        public async Task SaveSession()
        {
            await Data.TextChannel.SendMessageAsync(Data.SaveSessionMessage());
        }

        #endregion
    }
}