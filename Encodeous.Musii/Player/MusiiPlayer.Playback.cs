﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using Encodeous.Musii.Core;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Playback Methods
        
        public Task SetVolumeAsync(int vol)
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MVolume, new
                {
                    BeforeState = State,
                    NewVolume = vol
                });
                State.Volume = vol;
                await _guild.Node.SetVolumeAsync(vol);
            });
        }

        public Task ShuffleAsync()
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MShuffle, new
                {
                    BeforeState = State
                });
                State.Tracks.Shuffle();
            });
        }

        public Task<bool> TogglePauseAsync()
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MPause, new
                {
                    BeforeState = State
                });
                if (State.IsPaused)
                {
                    await _guild.Node.ResumeAsync();
                }
                else
                {
                    await _guild.Node.PauseAsync();
                }

                State.IsPaused = !State.IsPaused;
                return State.IsPaused;
            });
        }
        
        public Task<bool> ToggleLockAsync()
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MLock, new
                {
                    BeforeState = State
                });
                State.IsLocked = !State.IsLocked;
                return State.IsLocked;
            });
        }
        
        public Task<LoopType> SetLoopTypeAsync(LoopType loopType)
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MLoop, new
                {
                    BeforeState = State,
                    NewLoopType = loopType
                });
                return State.Loop = loopType;
            });
        }
        
        public Task<AudioFilter> SetFilterTypeAsync(AudioFilter filterType)
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MFilter, new
                {
                    BeforeState = State,
                    NewFilterType = filterType
                });
                if (filterType == AudioFilter.None)
                {
                    await _guild.Node.ResetEqualizerAsync();
                    State.Filter = AudioFilter.None;
                }
                else
                {
                    var filter = filterType switch
                    {
                        AudioFilter.Bass => Constants.PUNCH_BASS,
                        AudioFilter.Piano => Constants.PIANO,
                        AudioFilter.Metal => Constants.METAL_ROCK,
                        _ => throw new Exception("Invalid filter")
                    };
                    await _guild.Node.AdjustEqualizerAsync(filter);
                    State.Filter = filterType;
                }
                return filterType;
            });
        }

        /// <summary>
        /// 0 based index
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public Task<bool> SkipSongsAsync(int l, int r)
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MSkip, new
                {
                    BeforeState = State,
                    BeforeTrackCount = State.Tracks.Count,
                    l,
                    r,
                    IsValid = !(l > r || l < 0 || r > State.Tracks.Count),
                    SkipCurrent = l == 0
                });
                if (l > r || l < 0 || r > State.Tracks.Count) return false;

                int nl = Math.Max(0, l - 1);
                int nr = r - 1;
                State.Tracks.RemoveRange(nl, nr - nl + 1);
                if (l == 0)
                {
                    // skip current song also
                    await MoveNextUnlockedAsync();
                    await PlayActiveSongAsync();
                }
                return true;
            });
        }
        
        public Task<bool> JumpAsync(int count)
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MJump, new
                {
                    BeforeState = State,
                    BeforeTrackCount = State.Tracks.Count,
                    JumpCount = count,
                    JumpRem = count % (State.Tracks.Count + 1)
                });

                int rjump = count % (State.Tracks.Count + 1);
                if (rjump != 0)
                {
                    if (rjump < 0)
                    {
                        int ridx = State.Tracks.Count + rjump;
                        var rsel = State.Tracks.GetRange(ridx, State.Tracks.Count - ridx);
                        rsel.Add(State.CurrentTrack.Clone());
                        State.Tracks.RemoveRange(ridx, State.Tracks.Count - ridx);
                        State.Tracks.InsertRange(0, rsel);
                    }
                    else
                    {
                        var rsel = State.Tracks.GetRange(0,rjump-1);
                        rsel.Insert(0, State.CurrentTrack.Clone());
                        State.Tracks.RemoveRange(0, rjump-1);
                        State.Tracks.AddRange(rsel);
                    }
                    await MoveNextUnlockedAsync();
                    await PlayActiveSongAsync();
                }
               
                return true;
            });
        }

        public async Task AddTracksAsync(BaseMusicSource[] tracks, CommandContext ctx = null)
        {
            await _guild.Trace(TraceSource.MAdd, new
            {
                CurrentState = State,
                TrackCount = tracks.Length
            });
            if (tracks.Length == 1)
            {
                if (ctx != null)
                    await ctx.RespondAsync(_guild.AddedTrackMessage(await tracks[0].GetTrack(_guild.Node)));
                else await Text.SendMessageAsync(_guild.AddedTrackMessage(await tracks[0].GetTrack(_guild.Node)));
            }
            else
            {
                if (ctx != null) await ctx.RespondAsync(_guild.AddedTracksMessage(tracks.Length));
                else await Text.SendMessageAsync(_guild.AddedTracksMessage(tracks.Length));
            }

            await ExecuteSynchronized(() =>
            {
                State.Tracks.AddRange(tracks);
                return Task.CompletedTask;
            });
        }
        
        #endregion
    }
}