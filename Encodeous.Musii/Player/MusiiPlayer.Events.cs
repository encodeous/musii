﻿using System.Threading.Tasks;
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
            await _manager.Trace(TraceSource.LLWebsocketClose, new
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
            await Stop(true);
        }

        public async Task PlaybackFinished(TrackFinishEventArgs args)
        {
            if (args.Reason == TrackEndReason.Replaced || args.Reason == TrackEndReason.Cleanup) return;
            await _manager.Trace(TraceSource.LLPlaybackFinish, new
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
                    await this.ExecuteSynchronized(async () =>
                    {
                        await _manager.Trace(TraceSource.MLoop, new
                        {
                            BeforeState = State
                        });
                        State.Tracks.Add(new YoutubeLazySource(State.CurrentTrack));
                    });
                }
                else
                {
                    await this.ExecuteSynchronized(async () =>
                    {
                        await _manager.Trace(TraceSource.MLoop, new
                        {
                            BeforeState = State
                        });
                        State.Tracks.Insert(0, new YoutubeLazySource(State.CurrentTrack));
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
            await _manager.Trace(TraceSource.LLTrackException, new
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
            await _manager.Trace(TraceSource.LLTrackStuck, new
            {
                CurrentState = State,
                args
            });
            await Task.Delay(1000);
            await PlayActiveSongAsync();
            _log.LogDebug($"Track stuck in channel {args.Player.Channel}");
        }
        
        public void TrackUpdated(PlayerUpdateEventArgs args)
        {
            if (args.Player.CurrentState.CurrentTrack == State.CurrentTrack)
            {
                _manager.Trace(TraceSource.LLTrackUpdated, new
                {
                    CurrentState = State,
                    args.Position,
                    args.Timestamp,
                    args.Handled
                }).WaitAndUnwrapException();
                this.ExecuteSynchronized(() =>
                {
                    State.CurrentTrack.SetPos((long) args.Position.TotalMilliseconds);
                    return Task.CompletedTask;
                }, true).WaitAndUnwrapException();
            }
        }
        
        #endregion
    }
}