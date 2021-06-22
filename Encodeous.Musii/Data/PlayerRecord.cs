using System;
using System.Collections.Generic;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// An immutable record of a player's state
    /// </summary>
    public record PlayerRecord
    {
        public Guid RecordId { get; } = Guid.NewGuid();
        public bool IsLooped { get; init; } = false;
        public int Volume { get; init; } = 100;
        public LavalinkTrack CurrentTrack { get; init; } = null;
        public IReadOnlyList<IMusicSource> Tracks { get; init; } = new List<IMusicSource>();
        public bool IsPaused { get; init; } = false;
    }
}