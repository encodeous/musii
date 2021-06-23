using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Network;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// An immutable record of a player's state
    /// </summary>
    public record PlayerRecord
    {
        [Key]
        public Guid RecordId { get; } = Guid.NewGuid();
        public LoopType Loop { get; init; } = LoopType.Off;
        public int Volume { get; init; } = 100;
        public BaseMusicSource CurrentTrack { get; init; } = null;
        public TimeSpan CurrentPosition { get; init; } = TimeSpan.Zero;
        public IReadOnlyList<BaseMusicSource> Tracks { get; init; } = new List<BaseMusicSource>();
    }
}