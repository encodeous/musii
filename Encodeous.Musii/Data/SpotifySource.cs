using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using SpotifyAPI.Web;

namespace Encodeous.Musii.Data
{
    public class SpotifySource : IMusicSource
    {
        private bool _hasQueried = false;
        private LavalinkTrack _cachedTrack = null;
        private string _query;
        private string _title;

        /// <summary>
        /// This constructor is meant for cloning
        /// </summary>
        public SpotifySource()
        { }
        public SpotifySource(FullTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            _query = query;
            _title = track.Name;
        }
        public SpotifySource(SimpleTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            _query = query;
            _title = track.Name;
        }

        public async Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            if (_hasQueried) return _cachedTrack;
            _hasQueried = true;
            var res = await connection.GetTracksAsync(_query);
            return _cachedTrack = res.Tracks.First();
        }

        public string GetTrackName()
        {
            return _title;
        }

        public IMusicSource CloneSource()
        {
            return new SpotifySource()
            {
                _query = _query,
                _title = new string(_title),
                _cachedTrack = _cachedTrack?.CloneTrack()
            };
        }
    }
}