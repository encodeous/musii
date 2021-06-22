using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Encodeous.Musii.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace Encodeous.Musii.Network
{
    public class SpotifyService
    {
        private SpotifyClient _client;

        public SpotifyService(IConfiguration appConfig, ILogger<SpotifyService> log)
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(appConfig["musii:SpotifyClientId"], appConfig["musii:SpotifyClientSecret"]));
            _client = new SpotifyClient(config);
            log.LogInformation("Connected to Spotify API");
        }
        public string ParsePlaylist(string url)
        {
            var rx = new Regex("open.spotify.com/playlist/[0-9A-z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(26);
        }

        public string ParseAlbum(string url)
        {
            var rx = new Regex("open.spotify.com/album/[0-9A-z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(23);
        }

        public string ParseTrack(string url)
        {
            var rx = new Regex("open.spotify.com/track/[0-9A-z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(23);
        }

        public async Task<FullPlaylist> GetPlaylist(string url)
        {
            string id = ParsePlaylist(url);
            if (id == "") return null;
            try
            {
                return await _client.Playlists.Get(id);
            }
            catch
            {

            }
            return null;
        }
        public async Task<FullAlbum> GetAlbum(string url)
        {
            string id = ParseAlbum(url);
            if (id == "") return null;
            try
            {
                return await _client.Albums.Get(id);
            }
            catch
            {

            }
            return null;
        }
        public async Task<FullTrack> GetTrack(string url)
        {
            string id = ParseTrack(url);
            if (id == "") return null;
            try
            {
                return await _client.Tracks.Get(id);
            }
            catch
            {

            }
            return null;
        }
        
        public SpotifySource CreateSpotifyTrack(FullTrack track)
        {
            return new (track);
        }
        
        public SpotifySource[] CreateSpotifyAlbum(FullAlbum album)
        {
            List<SpotifySource> requests = new List<SpotifySource>();
            var tracks = album.Tracks.Items;
            int cnt = 0;
            foreach (var track in tracks)
            {
                requests.Add(new (track));
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }
            return requests.ToArray();
        }
        
        public SpotifySource[] CreateSpotifyPlaylist(FullPlaylist album)
        {
            List<SpotifySource> requests = new List<SpotifySource>();
            var tracks = album.Tracks.Items;
            int cnt = 0;
            foreach (var ptrack in tracks)
            {
                if (ptrack.Track is FullTrack track)
                {
                    requests.Add(new (track));
                    cnt++;
                    if (cnt >= 500)
                    {
                        break;
                    }
                }
            }
            return requests.ToArray();
        }
    }
}