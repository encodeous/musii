using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// Represents the state of a player, could be moved across guilds
    /// TODO: Add expiry time to states
    /// </summary>
    public class PlayerState
    {
        public Guid StateId = Guid.NewGuid();
        public bool IsLooped = false;
        public double Volume = 100;
        public LavalinkTrack CurrentTrack = null;
        public List<IMusicSource> Tracks = new List<IMusicSource>();
        public SemaphoreSlim StateLock = new (1,1);

        public PlayerState CloneState()
        {
            return new()
            {
                Volume = Volume,
                IsLooped = IsLooped,
                CurrentTrack = CurrentTrack?.CloneTrack(),
                Tracks = Tracks.Select(x => x.CloneSource()).ToList()
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is null || obj.GetType() != typeof(PlayerState)) return false;
            return ((PlayerState) obj).StateId == StateId;
        }
    }
}