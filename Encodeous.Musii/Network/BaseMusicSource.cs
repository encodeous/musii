using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Encodeous.Musii.Network
{
    /// <summary>
    /// Represents a music source over the network
    /// </summary>
    public abstract class BaseMusicSource
    {
        public abstract Task<LavalinkTrack> GetTrack(LavalinkGuildConnection connection);
        public abstract string GetTrackName();
    }
}