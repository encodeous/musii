using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Network;

namespace Encodeous.Musii.Search
{
    public class YoutubeService
    {
        public async Task<YoutubeSource[]> SearchPlaylistAsync(string link, LavalinkGuildConnection conn)
        {
            var videos = await conn.GetTracksAsync(new Uri(link));
            if (videos.LoadResultType != LavalinkLoadResultType.PlaylistLoaded)
                throw new Exception("Unexpected result returned");
            return videos.Tracks.Select(x=>new YoutubeSource(x, false)).ToArray();
        }
        
        public async Task<YoutubeSource> SearchVideoAsync(string link, LavalinkGuildConnection conn)
        {
            var videos = await conn.GetTracksAsync(new Uri(link));
            if (videos.LoadResultType != LavalinkLoadResultType.TrackLoaded)
                throw new Exception("Unexpected result returned");
            return new (videos.Tracks.First(), false);
        }
        public async Task<YoutubeSource> SearchVideoAsync(string[] keywords, LavalinkGuildConnection conn)
        {
            var videos = await conn.GetTracksAsync(string.Join(' ', keywords));
            if (videos.LoadResultType != LavalinkLoadResultType.SearchResult)
                throw new Exception("Unexpected result returned");
            return new (videos.Tracks.First(), false);
        }
    }
}