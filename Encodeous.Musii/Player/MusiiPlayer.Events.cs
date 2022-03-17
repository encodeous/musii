using System.Threading.Tasks;
using DSharpPlus.Lavalink.EventArgs;
using Encodeous.Musii.Core;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx.Synchronous;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Lavalink Lifetime Events

        public async Task WsClosed(WebSocketCloseEventArgs args)
        {
            await _guild.Trace(TraceSource.LLWebsocketClose, new
            {
                CurrentState = State,
                args
            });
            if (args.Code == 4014)
            {
                await Text.SendMessageAsync(Messages.GenericError(
                    "Disconnected by Moderator", "The bot will leave", ""));
                _log.LogDebug($"Bot voice websocket closed {args.Reason} with code {args.Code}");
                
            }
            else
            {
                await Text.SendMessageAsync(Messages.GenericError(
                    "Network Error", "The bot has encountered a network error", ""));
            }
            await StopAsync(true);
        }

        public async Task PlaybackFinished(TrackFinishEventArgs args)
        {
            if (args.Reason == TrackEndReason.Replaced || args.Reason == TrackEndReason.Cleanup) return;
            await _guild.Trace(TraceSource.LLPlaybackFinish, new
            {
                CurrentState = State,
                args.Track,
                args.Reason,
                args.Handled,
                State.Loop
            });
            if (State.Loop != LoopType.Off)
            {
                if (State.Loop == LoopType.Playlist)
                {
                    await ExecuteSynchronized(async () =>
                    {
                        await _guild.Trace(TraceSource.MLoop, new
                        {
                            BeforeState = State
                        });
                        State.Tracks.Add(State.CurrentTrack.Clone());
                    });
                }
                else
                {
                    await ExecuteSynchronized(async () =>
                    {
                        await _guild.Trace(TraceSource.MLoop, new
                        {
                            BeforeState = State
                        });
                        State.Tracks.Insert(0, State.CurrentTrack.Clone());
                    });
                }
            }
            if (args.Reason == TrackEndReason.LoadFailed)
            {
                await Text.SendMessageAsync(Messages.GenericError("Track load failed",
                    $"The track `{args.Track.Title}` is not able to be played!", ""));
            }
            if (await MoveNextAsync())
            {
                await PlayActiveSongAsync();
            }
        }

        public async Task TrackException(TrackExceptionEventArgs args)
        {
            await _guild.Trace(TraceSource.LLTrackException, new
            {
                CurrentState = State,
                args
            });
            await Task.Delay(1000);
            await PlayActiveSongAsync();
            _log.LogError($"Playback encountered error {args.Error} in channel {args.Player.Channel}");
        }
        
        public async Task TrackStuck(TrackStuckEventArgs args)
        {
            await _guild.Trace(TraceSource.LLTrackStuck, new
            {
                CurrentState = State,
                args
            });
            await Task.Delay(1000);
            await PlayActiveSongAsync();
            _log.LogDebug($"Track stuck in channel {args.Player.Channel}");
        }
        
        public async Task TrackUpdated(PlayerUpdateEventArgs args)
        {
            if (State.CurrentTrack is null) return;
            var track = await _guild.ResolveTrackAsync(State.CurrentTrack);
            if (args.Player.CurrentState.CurrentTrack == track)
            {
                _guild.Trace(TraceSource.LLTrackUpdated, new
                {
                    CurrentState = State,
                    args.Position,
                    args.Timestamp,
                    args.Handled
                }).WaitAndUnwrapException();
                State.CurrentPosition = args.Position;
            }
        }
        
        #endregion
    }
}