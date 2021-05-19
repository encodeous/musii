using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Data
{
    public class YoutubeSource : IMusicSource
    {
        private LavalinkTrack _track;
        /// <summary>
        /// This constructor is meant for cloning
        /// </summary>
        public YoutubeSource()
        { }
        public YoutubeSource(LavalinkTrack video)
        {
            _track = video;
        }
        public Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            return Task.FromResult(_track);
        }

        public string GetTrackName()
        {
            return _track.Title;
        }

        public IMusicSource CloneSource()
        {
            return new YoutubeSource()
            {
                _track = _track.CloneTrack()
            };
        }
    }
}