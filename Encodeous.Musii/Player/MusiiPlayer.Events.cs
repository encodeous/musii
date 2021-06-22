using System.Threading.Tasks;
using DSharpPlus.Lavalink.EventArgs;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Lavalink Lifetime Events

        public async Task WsClosed(WebSocketCloseEventArgs args)
        {
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
            if (args.Reason == TrackEndReason.Finished)
            {
                if (await MoveNextAsync())
                {
                    await PlayActiveSongAsync();
                }
            }
            if (args.Reason == TrackEndReason.LoadFailed)
            {
                await Text.SendMessageAsync(Messages.GenericError("Track load failed", 
                    $"The track `{args.Track.Title}` is not able to be played!", ""));
                if (await MoveNextAsync())
                {
                    await PlayActiveSongAsync();
                }
            }
        }
        
        public async Task TrackException(TrackExceptionEventArgs args)
        {
            await Task.Delay(1000);
            await PlayActiveSongAsync();
            _log.LogError($"Playback encountered error {args.Error} in channel {args.Player.Channel}");
        }
        
        public async Task TrackStuck(TrackStuckEventArgs args)
        {
            await Task.Delay(1000);
            await PlayActiveSongAsync();
            _log.LogDebug($"Track stuck in channel {args.Player.Channel}");
        }
        
        public void TrackUpdated(PlayerUpdateEventArgs args)
        {
            State.CurrentTrack.SetPos((long) args.Position.TotalMilliseconds);
            State.QueueUpdate.Set();
            State.QueueUpdate.Reset();
        }
        
        #endregion
    }
}