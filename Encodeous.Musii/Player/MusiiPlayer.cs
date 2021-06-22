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

        private DiscordMessage _queueMessage = null;
        private ILogger _log;
        private MusiiGuild _manager;
        private bool _stopped = false;
        private int _queueLength;
        private TimeSpan _queueTimeout;

        #endregion
        
        internal DiscordClient Client;
        internal PlayerState State;
        internal DiscordChannel Voice, Text;

        public MusiiPlayer(ILogger log, MusiiGuild manager, DiscordClient client, 
            PlayerRecord record, DiscordChannel voice, DiscordChannel text, TimeSpan unpinnnedLeaveTime,
            int queueLength, TimeSpan queueTimeout)
        {
            _log = log;
            _manager = manager;
            Client = client;
            State = new PlayerState(record);
            Voice = voice;
            Text = text;
            _queueLength = queueLength;
            _queueTimeout = queueTimeout;
            Task.Run(async () =>
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
                            await Stop(true, true);
                        }
                    }

                    await Task.Delay(1000);
                }
            });
            if (State.IsPaused)
            {
                _manager.Node.PauseAsync().WaitAndUnwrapException();
            }
        }

        #endregion
        
        public async Task Stop(bool saveRecord = false, bool unpinnedLeave = false)
        {
            await _manager.Trace(TraceSource.MStop, new
            {
                saveRecord
            });
            _stopped = true;
            if (saveRecord)
            {
                await Text.SendMessageAsync(_manager.SaveSessionMessage(unpinnedLeave));
            }
            await _manager.StopAsync(true);
        }
        public Task<bool> MoveNextAsync()
        {
            return this.ExecuteSynchronized(MoveNextAsyncUnlocked, true);
        }
        private async Task<bool> MoveNextAsyncUnlocked()
        {
            await _manager.Trace(TraceSource.MMoveNext, new
            {
                BeforeState = State
            });
            if (!State.Tracks.Any())
            {
                await Text.SendMessageAsync(_manager.PlaylistEmptyMessage());
                await Stop();
                return false;
            }
            // Fetch track
            State.CurrentTrack = await State.Tracks.First().GetTrack(_manager.Node);
            State.Tracks.RemoveAt(0);
            return true;
        }
        public async Task SetPosition(TimeSpan pos)
        {
            await this.ExecuteSynchronized(async () =>
            {
                await _manager.Trace(TraceSource.MPlayPartialActive, new
                {
                    CurrentState = State,
                    NewPos = pos
                });
                State.CurrentTrack.SetPos((long)pos.TotalMilliseconds);
                await _manager.Node.PlayPartialAsync(State.CurrentTrack, pos, State.CurrentTrack.Length);
                if (State.IsPaused) await _manager.Node.PauseAsync();
            }, true);
        }
        public async Task PlayActiveSongAsync()
        {
            await _manager.Trace(TraceSource.MPlayActive, new
            {
                CurrentState = State
            });
            await _manager.Node.PlayAsync(State.CurrentTrack);
        }
        public Task<bool> TogglePinAsync()
        {
            return this.ExecuteSynchronized(async () =>
            {
                await _manager.Trace(TraceSource.MPin, new
                {
                    BeforeState = State
                });
                State.IsPinned = !State.IsPinned;
                return State.IsPinned;
            });
        }
    }
}