using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Network
{
    public class YoutubeSource : IMusicSource
    {
        public LavalinkTrack Track;
        public YoutubeSource(LavalinkTrack video)
        {
            Track = video;
        }
        public Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection)
        {
            return Task.FromResult(Track);
        }

        public string GetTrackName()
        {
            return Track.Title;
        }
    }
}