using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Network;
using Newtonsoft.Json;
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
            Loop = record.Loop;
            if(record.CurrentTrack != null) CurrentTrack = record.CurrentTrack.Clone();
            CurrentPosition = record.CurrentPosition;
            StartTime = DateTime.UtcNow;
            Filter = AudioFilter.None;
        }
        // state-specific properties
        public int Volume = 100;
        public bool IsLocked;
        public AudioFilter Filter;
        [JsonIgnore]
        public readonly SemaphoreSlim StateLock = new (1,1);
        public bool IsPaused;
        public bool IsPinned;
        public DateTime StartTime;
        // data stored in records
        public LoopType Loop;
        public BaseMusicSource CurrentTrack;
        public TimeSpan CurrentPosition;
        [JsonIgnore]
        public List<BaseMusicSource> Tracks;

        /// <summary>
        /// Saves the current state as an immutable record
        /// </summary>
        /// <returns></returns>
        public PlayerRecord SaveRecord()
        {
            var mapped = Tracks.Select(x => x.Clone()).ToList();
            return new ()
            {
                Loop = Loop,
                CurrentTrack = CurrentTrack?.Clone(),
                CurrentPosition = CurrentPosition,
                Tracks = mapped
            };
        }
    }
}