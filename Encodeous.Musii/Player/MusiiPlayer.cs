using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Encodeous.Musii.Core;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Encodeous.Musii.Search;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx.Synchronous;

namespace Encodeous.Musii.Player
{
    /// <summary>
    /// Note to self:
    /// Records can be recorded any time
    /// Only one record can be loaded at one time
    /// Records can be shared across servers
    ///
    /// Absolutely NO Lavalink connection code should be in this class!
    /// </summary>
    public partial class MusiiPlayer
    {
        #region Props and Fields

        #region Private Data

        private DiscordMessage _queueMessage;
        private ILogger _log;
        private MusiiGuild _guild;
        private bool _stopped;
        private int _queueLength;
        private TimeSpan _queueTimeout;

        #endregion

        private DiscordClient _client;
        internal PlayerState State;
        internal DiscordChannel Voice, Text;

        public MusiiPlayer(ILogger log, MusiiGuild guild, DiscordClient client, 
            PlayerRecord record, DiscordChannel voice, DiscordChannel text, TimeSpan unpinnnedLeaveTime,
            int queueLength, TimeSpan queueTimeout)
        {
            _log = log;
            _guild = guild;
            _client = client;
            State = new PlayerState(record);
            Voice = voice;
            Text = text;
            _queueLength = queueLength;
            _queueTimeout = queueTimeout;
            Task.Run(async () =>
            {
                try
                {
                    DateTime leastTime = DateTime.UtcNow;
                    while (!_stopped)
                    {
                        if (!State.IsPinned)
                        {
                            if (Voice.Users.Count() > 1)
                            {
                                leastTime = DateTime.UtcNow;
                            }

                            if (DateTime.UtcNow - leastTime > unpinnnedLeaveTime)
                            {
                                await StopAsync(true, true);
                            }
                        }

                        await Task.Delay(1000);
                    }
                }
                catch
                {
                    try
                    {
                        await StopAsync(true, false);
                    }
                    catch
                    {
                        
                    }
                }
            });
            if (State.IsPaused)
            {
                _guild.Node.PauseAsync().WaitAndUnwrapException();
            }
        }

        #endregion
        
        public async Task StopAsync(bool saveRecord = false, bool unpinnedLeave = false)
        {
            await _guild.Trace(TraceSource.MStop, new
            {
                saveRecord
            });
            _stopped = true;
            if (saveRecord)
            {
                await Text.SendMessageAsync(_guild.SaveSessionMessage(unpinnedLeave));
            }
            await _guild.StopAsync(true);
        }
        public Task<bool> MoveNextAsync()
        {
            return ExecuteSynchronized(MoveNextUnlockedAsync);
        }
        private async Task<bool> MoveNextUnlockedAsync()
        {
            await _guild.Trace(TraceSource.MMoveNext, new
            {
                BeforeState = State
            });
            State.CurrentTrack = null;
            State.CurrentPosition = TimeSpan.Zero;
            if (!State.Tracks.Any())
            {
                await Text.SendMessageAsync(_guild.PlaylistEmptyMessage());
                await StopAsync();
                
                return false;
            }
            // Fetch track
            State.CurrentTrack = State.Tracks.First();
            State.Tracks.RemoveAt(0);
            return true;
        }
        public async Task SetPositionAsync(TimeSpan pos)
        {
            await ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MPlayPartialActive, new
                {
                    CurrentState = State,
                    NewPos = pos
                });
                State.CurrentPosition = pos;
                var track = await _guild.ResolveTrackAsync(State.CurrentTrack);
                await _guild.Node.PlayPartialAsync(track, pos, track.Length);
            });
        }
        public async Task PlayActiveSongAsync()
        {
            if (State.CurrentTrack == null) return;
            await _guild.Trace(TraceSource.MPlayActive, new
            {
                CurrentState = State
            });
            var track = await _guild.ResolveTrackAsync(State.CurrentTrack);
            if (State.CurrentPosition != TimeSpan.Zero)
            {
                await _guild.Node.PlayPartialAsync(track, State.CurrentPosition, track.Length);
            }
            else
            {
                await _guild.Node.PlayAsync(track);
            }
        }
        public Task<bool> TogglePinAsync()
        {
            return ExecuteSynchronized(async () =>
            {
                await _guild.Trace(TraceSource.MPin, new
                {
                    BeforeState = State
                });
                State.IsPinned = !State.IsPinned;
                return State.IsPinned;
            });
        }
        
        public async Task<T> ExecuteSynchronized<T>(Func<Task<T>> func)
        {
            await State.StateLock.WaitAsync();
            try
            {
                return await func();
            }
            finally
            {
                State.StateLock.Release();
            }
        }
        public async Task ExecuteSynchronized(Func<Task> func)
        {
            await State.StateLock.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                State.StateLock.Release();
            }
        }
    }
}