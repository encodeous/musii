using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Encodeous.Musii.Network
{
    public class YoutubeSource : BaseMusicSource
    {
        public LavalinkTrack Track;

        [JsonConstructor]
        public YoutubeSource()
        {
            
        }
        public YoutubeSource(LavalinkTrack video)
        {
            Track = video;
        }
        public override Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            return Task.FromResult(Track);
        }

        public override string GetTrackName()
        {
            return Track.Title;
        }
    }
}