using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using Encodeous.Musii.Network;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Playback Methods
        
        public Task SetVolume(int vol)
        {
            return this.ExecuteSynchronized(async () =>
            {
                State.Volume = vol;
                await _manager.Node.SetVolumeAsync(vol);
            }, true);
        }

        public Task ShuffleAsync()
        {
            return this.ExecuteSynchronized(async () => { State.Tracks.Shuffle(); }, true);
        }

        public Task<bool> TogglePause()
        {
            return this.ExecuteSynchronized(async () =>
            {
                if (State.IsPaused)
                {
                    await _manager.Node.ResumeAsync();
                }
                else
                {
                    await _manager.Node.PauseAsync();
                }

                State.IsPaused = !State.IsPaused;
                return State.IsPaused;
            }, true);
        }
        
        public Task<bool> ToggleLock()
        {
            return this.ExecuteSynchronized(() =>
            {
                State.IsLocked = !State.IsLocked;
                return Task.FromResult(State.IsLocked);
            });
        }

        public Task<bool> SkipSongs(int l, int r)
        {
            return this.ExecuteSynchronized(async () =>
            {
                if (l > r || l < 0 || r > State.Tracks.Count) return false;

                int nl = Math.Max(0, l - 1);
                int nr = r - 1;
                State.Tracks.RemoveRange(nl, nr - nl + 1);
                if (l == 0)
                {
                    // skip current song also
                    await MoveNextAsyncUnlocked();
                    await PlayActiveSongAsync();
                }
                return true;
            }, true);
        }

        public async Task AddTracks(IMusicSource[] tracks, CommandContext ctx = null)
        {
            if (tracks.Length == 1)
            {
                if (ctx != null)
                    await ctx.RespondAsync(_manager.AddedTrackMessage(await tracks[0].GetTrack(_manager.Node)));
                else await Text.SendMessageAsync(_manager.AddedTrackMessage(await tracks[0].GetTrack(_manager.Node)));
            }
            else
            {
                if (ctx != null) await ctx.RespondAsync(_manager.AddedTracksMessage(tracks.Length));
                else await Text.SendMessageAsync(_manager.AddedTracksMessage(tracks.Length));
            }

            await this.ExecuteSynchronized(() =>
            {
                State.Tracks.AddRange(tracks);
                return Task.CompletedTask;
            }, true);
        }
        
        #endregion
    }
}