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
            Volume = record.Volume;
            if(record.CurrentTrack != null) CurrentTrack = record.CurrentTrack.CloneTrack();
            StartTime = DateTime.UtcNow;
            Filter = AudioFilter.None;
        }
        public LoopType Loop;
        public bool IsLocked;
        public int Volume;
        public AudioFilter Filter;
        public LavalinkTrack CurrentTrack;
        [JsonIgnore]
        public List<BaseMusicSource> Tracks;
        public AsyncManualResetEvent QueueUpdate = new(false);
        [JsonIgnore]
        public readonly SemaphoreSlim StateLock = new (1,1);
        public bool IsPaused;
        public bool IsPinned;
        public DateTime StartTime;

        /// <summary>
        /// Saves the current state as an immutable record
        /// </summary>
        /// <returns></returns>
        public PlayerRecord SaveRecord()
        {
            var mapped = Tracks.Select(x =>
            {
                if (x is YoutubeSource x1)
                {
                    return (BaseMusicSource) new YoutubeLazySource(x1.Track);
                }
                else if (x is YoutubeLazySource x2)
                {
                    return new YoutubeLazySource(x2.TrackId, x2.Title);
                }
                else if (x is SpotifySource x3)
                {
                    return new SpotifySource()
                    {
                        Title = x3.Title,
                        BuiltQuery = x3.BuiltQuery,
                        HasQueried = false,
                        Track = null
                    };
                }
                else
                {
                    throw new Exception("Invalid Track Type");
                }
            }).ToList();
            if (CurrentTrack is not null)
            {
                mapped.Insert(0, new YoutubeLazySource(CurrentTrack));
            }
            return new ()
            {
                Loop = Loop,
                Volume = Volume,
                CurrentTrack = null,
                Tracks = mapped
            };
        }
    }
}