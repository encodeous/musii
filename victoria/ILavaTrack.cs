using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Victoria
{
    public interface ILavaTrack
    {
        /// <summary>
        ///     Track's author.
        /// </summary>
        string Author { get; }

        /// <summary>
        ///     Whether the track is seekable.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        ///     Track's length.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Track's encoded hash.
        /// </summary>
        string Hash { get; }

        /// <summary>
        ///     Audio / Video track Id.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     Whether the track is a stream.
        /// </summary>
        bool IsStream { get; }

        /// <summary>
        ///     Track's current position.
        /// </summary>
        TimeSpan Position { get; set; }

        /// <summary>
        ///     Track's title.
        /// </summary>
        string Title { get; }

        /// <summary>
        ///     Track's url.
        /// </summary>
        string Url { get; }

        /// <summary>
        ///     Track's Thumbnail url.
        /// </summary>
        string ThumbnailUrl { get; set; }

        IMessage TrackMessage { get; set; }
    }
}
