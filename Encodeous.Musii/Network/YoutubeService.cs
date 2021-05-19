using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Encodeous.Musii.Data;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Network
{
    public class YoutubeService
    {
        private HttpClient _client;
        private YoutubeClient _ytClient;

        public void InitializeClient(IPEndPoint proxy)
        {
            _client = new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy(proxy.Address.ToString(), proxy.Port)
            });
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.UserAgent);
            _ytClient = new YoutubeClient(_client);
        }
        
        public void InitializeClient(HttpClient client)
        {
            _client = client;
            _ytClient = new YoutubeClient(_client);
        }

        public YoutubeClient GetClient()
        {
            return _ytClient;
        }
        
        public async Task<Track[]> SearchPlaylist(string link)
        {
            var playlist = PlaylistId.TryParse(link).Value;
            var videos = _ytClient.Playlists.GetVideosAsync(playlist.Value);
            int cnt = 0;

            List<Track> requests = new List<Track>();

            await foreach (var playlistVideo in videos)
            {
                requests.Add(new Track()
                {
                    Source = new YoutubeSource(playlistVideo),
                    Position = TimeSpan.Zero
                });
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }

            return requests.ToArray();
        }
        
        public Track SearchVideo(string link)
        {
            var id = VideoId.TryParse(link).Value;

            return new Track() {Source = new YoutubeLazySource(id), Position = TimeSpan.Zero};
        }
        public async Task<Track> SearchVideo(string[] keywords)
        {
            string query = string.Join(' ', keywords);
            var videos = _ytClient.Search.GetVideosAsync(query);
            var enu = videos.GetAsyncEnumerator();
            await enu.MoveNextAsync();
            var result = enu.Current;
            return new Track() {Source = new YoutubeSource(result), Position = TimeSpan.Zero};
        }
    }
}