using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Network
{
    /// <summary>
    /// Represents a music source over the network
    /// </summary>
    public interface IMusicSource
    {
        public Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection);
        public string GetTrackName();
    }
}