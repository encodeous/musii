using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Network
{
    public class YoutubeSource : IMusicSource
    {
        private LavalinkTrack _track;
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
    }
}