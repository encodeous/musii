using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Encodeous.Musii.Network
{
    public class YoutubeLazySource : BaseMusicSource
    {
        public string TrackId;
        public string Title;
        private LavalinkTrack _privateTrack = null;

        [JsonConstructor]
        public YoutubeLazySource()
        {
            
        }
        public YoutubeLazySource(string id, string title)
        {
            TrackId = id;
            Title = title;
        }
        public YoutubeLazySource(LavalinkTrack track)
        {
            TrackId = track.Identifier;
            Title = track.Title;
        }
        public override async Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            if (_privateTrack is null)
            {
                var x = await connection.GetTracksAsync(TrackId);
                _privateTrack = x.Tracks.First();
            }

            return _privateTrack;
        }

        public override string GetTrackName()
        {
            return Title;
        }
    }
}