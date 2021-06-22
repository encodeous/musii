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

        #endregion
        
        internal DiscordClient Client;
        internal PlayerState State;
        internal DiscordChannel Voice, Text;

        public MusiiPlayer(ILogger log, MusiiGuild manager, DiscordClient client, PlayerRecord record, DiscordChannel voice, DiscordChannel text)
        {
            _log = log;
            _manager = manager;
            Client = client;
            State = new PlayerState(record);
            Voice = voice;
            Text = text;
        }

        #endregion
        
        public async Task Stop(bool saveRecord = false)
        {
            _stopped = true;
            if (saveRecord)
            {
                await Text.SendMessageAsync(_manager.SaveSessionMessage());
            }
            await _manager.StopAsync(true);
        }
        public Task<bool> MoveNextAsync()
        {
            return this.ExecuteSynchronized(async () =>
            {
                if (!State.Tracks.Any())
                {
                    await Text.SendMessageAsync(_manager.PlaylistEmptyMessage());
                    await Stop();
                    return false;
                }

                var cur = State.CurrentTrack;
                // Fetch track
                State.CurrentTrack = await State.Tracks.First().GetTrack(_manager.Node);
                State.Tracks.RemoveAt(0);
                if (State.IsLooped && cur is not null)
                {
                    cur.SetPos(0);
                    State.Tracks.Add(new YoutubeSource(cur));
                }

                return true;
            }, true);
        }
        private async Task<bool> MoveNextAsyncUnlocked()
        {
            if (!State.Tracks.Any())
            {
                await Text.SendMessageAsync(_manager.PlaylistEmptyMessage());
                await Stop();
                return false;
            }

            var cur = State.CurrentTrack;
            // Fetch track
            State.CurrentTrack = await State.Tracks.First().GetTrack(_manager.Node);
            State.Tracks.RemoveAt(0);
            if (State.IsLooped && cur is not null)
            {
                cur.SetPos(0);
                State.Tracks.Add(new YoutubeSource(cur));
            }
            return true;
        }
        public async Task SetPosition(TimeSpan pos)
        {
            await this.ExecuteSynchronized(async () =>
            {
                State.CurrentTrack.SetPos((long)pos.TotalMilliseconds);
                await _manager.Node.PlayPartialAsync(State.CurrentTrack, pos, State.CurrentTrack.Length);
                if (State.IsPaused) await _manager.Node.PauseAsync();
            }, true);
        }
        public async Task PlayActiveSongAsync()
        {
            await _manager.Node.PlayAsync(State.CurrentTrack);
        }
    }
}