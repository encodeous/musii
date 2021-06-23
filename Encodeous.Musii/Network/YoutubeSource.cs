using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Encodeous.Musii.Network
{
    public class YoutubeSource : BaseMusicSource
    {
        public string TrackId;
        public string Title;
        private LavalinkTrack _cachedTrack = null;

        [JsonConstructor]
        public YoutubeSource()
        {
            
        }
        public YoutubeSource(string id, string title)
        {
            TrackId = id;
            Title = title;
        }
        public YoutubeSource(LavalinkTrack track, bool cloneMode = true)
        {
            TrackId = track.Identifier;
            Title = track.Title;
            if (!cloneMode)
            {
                _cachedTrack = track;
            }
        }
        public override async Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            if (_cachedTrack is null)
            {
                var x = await connection.GetTracksAsync(TrackId);
                _cachedTrack = x.Tracks.First();
            }

            return _cachedTrack;
        }

        public override string GetTrackName()
        {
            return Title;
        }

        public override BaseMusicSource Clone()
        {
            return new YoutubeSource(TrackId, Title);
        }
    }
}