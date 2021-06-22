using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DSharpPlus.Lavalink;
using Nito.AsyncEx;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// Represents the state of a player, could be moved across guilds
    /// TODO: Add expiry time to states
    /// </summary>
    public class PlayerState
    {
        public PlayerState(PlayerRecord record)
        {
            Tracks = record.Tracks.ToList();
            IsLooped = record.IsLooped;
            Volume = record.Volume;
            if(record.CurrentTrack != null) CurrentTrack = record.CurrentTrack.CloneTrack();
            IsPaused = record.IsPaused;
            StartTime = DateTime.UtcNow;
        }
        public bool IsLooped;
        public bool IsLocked;
        public int Volume;
        public LavalinkTrack CurrentTrack;
        public List<IMusicSource> Tracks;
        public AsyncManualResetEvent QueueUpdate = new(false);
        public readonly SemaphoreSlim StateLock = new (1,1);
        public bool IsPaused;
        public DateTime StartTime;

        /// <summary>
        /// Saves the current state as an immutable record
        /// </summary>
        /// <returns></returns>
        public PlayerRecord SaveRecord()
        {
            return new ()
            {
                IsLooped = IsLooped,
                Volume = Volume,
                CurrentTrack = CurrentTrack.CloneTrack(),
                Tracks = Tracks.ToArray(),
                IsPaused = IsPaused
            };
        }
    }
}