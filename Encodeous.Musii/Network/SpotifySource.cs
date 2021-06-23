using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace Encodeous.Musii.Network
{
    public class SpotifySource : BaseMusicSource
    {
        public bool HasQueried = false;
        private LavalinkTrack _cachedTrack = null;
        public string BuiltQuery;
        public string Title;
        [JsonConstructor]
        public SpotifySource()
        {
        }
        public SpotifySource(FullTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            BuiltQuery = query;
            Title = track.Name;
        }
        public SpotifySource(SimpleTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            BuiltQuery = query;
            Title = track.Name;
        }

        public override async Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            if (HasQueried) return _cachedTrack;
            HasQueried = true;
            var res = await connection.GetTracksAsync(BuiltQuery);
            return _cachedTrack = res.Tracks.First();
        }

        public override string GetTrackName()
        {
            return Title;
        }

        public override BaseMusicSource Clone()
        {
            return new SpotifySource()
            {
                BuiltQuery = BuiltQuery,
                HasQueried = false,
                Title = Title
            };
        }
    }
}