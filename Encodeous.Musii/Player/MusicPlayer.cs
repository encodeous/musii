using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.VoiceNext;
using Emzi0767.Utilities;
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
        public SearchService Searcher;
        public bool IsInitialized { get; private set; }
        public bool IsPlaying { get; private set; }

        public ScopeData Data;
        private bool _isDisposed = false;
        private ILogger<MusicPlayer> _log;
        private DiscordClient _client;
        public MusicPlayer(ILogger<MusicPlayer> log, SearchService searcher, ScopeData data, DiscordClient client)
        {
            _log = log;
            Searcher = searcher;
            Data = data;
            _client = client;
        }

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
            IsPlaying = true;
            Data.LavalinkNode.DiscordWebSocketClosed += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await SaveSession();
                if (args.Code == 4014)
                {
                    await Data.TextChannel.SendMessageAsync(Data.GenericError(
                        "Disconnected by Moderator", "The bot will leave"));
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
                        await Play();
                    }
                }
                if (args.Reason == TrackEndReason.LoadFailed)
                {
                    await Data.TextChannel.SendMessageAsync(Data.GenericError("Track load failed", 
                        $"The track `{args.Track.Title}` is not able to be played!"));
                    if (await MoveNextAsync())
                    {
                        await Play();
                    }
                }
            };
            Data.LavalinkNode.TrackException += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await Play();
                _log.LogError($"Playback encountered error {args.Error} in channel {args.Player.Channel}");
            };
            Data.LavalinkNode.TrackStuck += async (sender, args) =>
            {
                if (!ReferenceEquals(Data.LavalinkNode, sender)) return;
                await Play();
                _log.LogDebug($"Track stuck in channel {args.Player.Channel}");
            };
            Data.LavalinkNode.PlayerUpdated += (sender, args) =>
            {
                Data.State.CurrentTrack.SetPos((long) args.Position.TotalMilliseconds);
                return Task.CompletedTask;
            };
            if(Data.State.CurrentTrack is null) await MoveNextAsync();
            await Play();
        }
        
        private async Task PlaylistEndedAsync()
        {
            await Data.TextChannel.SendMessageAsync(Data.PlaylistEmptyMessage());
        }
        
        private async Task SaveSession()
        {
            await Data.TextChannel.SendMessageAsync(Data.SaveSessionMessage());
        }

        public async Task AddTracks(IMusicSource[] tracks)
        {
            if (tracks.Length == 1)
            {
                await Data.TextChannel.SendMessageAsync(Data.AddedTrackMessage(await tracks[0].GetTrack(Data.LavalinkNode)));
            }
            else
            {
                await Data.TextChannel.SendMessageAsync(Data.AddedTracksMessage(tracks.Length));
            }
            await Data.State.StateLock.WaitAsync();
            try
            {
                Data.State.Tracks.AddRange(tracks);
            }
            finally
            {
                Data.State.StateLock.Release();
            }
        }

        public async Task Play()
        {
            await Data.LavalinkNode.PlayPartialAsync(Data.State.CurrentTrack,
                Data.State.CurrentTrack.Position, Data.State.CurrentTrack.Length);
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
            }

            return true;
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
                try
                {
                    Data.LavalinkNode?.StopAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    
                }
                try
                {
                    Data.LavalinkNode?.DisconnectAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    
                }
            }
        }
    }
}