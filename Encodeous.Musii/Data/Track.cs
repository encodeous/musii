using System;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// Represents a single "song" or track
    /// </summary>
    public class Track
    {
        public IMusicSource Source { get; init; }
        public TimeSpan Position { get; set; }

        public Track CloneTrack()
        {
            return new()
            {
                Source = Source.CloneSource(),
                Position = Position.Add(TimeSpan.Zero)
            };
        }
    }
}