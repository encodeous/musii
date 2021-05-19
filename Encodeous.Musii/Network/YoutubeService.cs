using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Data;

namespace Encodeous.Musii.Network
{
    public class YoutubeService
    {
        public async Task<YoutubeSource[]> SearchPlaylist(string link, LavalinkGuildConnection _conn)
        {
            var videos = await _conn.GetTracksAsync(new Uri(link));
            Debug.Assert(videos.LoadResultType == LavalinkLoadResultType.PlaylistLoaded);
            return videos.Tracks.Select(x=>new YoutubeSource(x)).ToArray();
        }
        
        public async Task<YoutubeSource> SearchVideo(string link, LavalinkGuildConnection _conn)
        {
            var videos = await _conn.GetTracksAsync(new Uri(link));
            Debug.Assert(videos.LoadResultType == LavalinkLoadResultType.TrackLoaded);
            return new (videos.Tracks.First());
        }
        public async Task<YoutubeSource> SearchVideo(string[] keywords, LavalinkGuildConnection _conn)
        {
            var videos = await _conn.GetTracksAsync(string.Join(' ', keywords));
            Debug.Assert(videos.LoadResultType == LavalinkLoadResultType.SearchResult);
            return new (videos.Tracks.First());
        }
    }
}