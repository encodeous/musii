using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using SpotifyAPI.Web;

namespace Encodeous.Musii.Network
{
    public class SpotifySource : IMusicSource
    {
        private bool _hasQueried = false;
        public LavalinkTrack Track = null;
        public string BuiltQuery;
        private string _title;
        
        public SpotifySource(FullTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            BuiltQuery = query;
            _title = track.Name;
        }
        public SpotifySource(SimpleTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            BuiltQuery = query;
            _title = track.Name;
        }

        public async Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            if (_hasQueried) return Track;
            _hasQueried = true;
            var res = await connection.GetTracksAsync(BuiltQuery);
            return Track = res.Tracks.First();
        }

        public string GetTrackName()
        {
            return _title;
        }
    }
}