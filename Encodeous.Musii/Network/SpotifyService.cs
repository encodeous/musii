﻿using System;
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

        public SpotifyService(IConfiguration appConfig, ILogger<SpotifyClient> log)
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(appConfig["musii:SpotifyClientId"], appConfig["musii:SpotifyClientSecret"]));
            _client = new SpotifyClient(config);
            log.LogInformation("Connected to Spotify API");
        }
        public string ParsePlaylist(string url)
        {
            var rx = new Regex("open.spotify.com/playlist/[0-9a-zA-Z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(26);
        }

        public string ParseAlbum(string url)
        {
            var rx = new Regex("open.spotify.com/album/[0-9a-zA-Z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(23);
        }

        public string ParseTrack(string url)
        {
            var rx = new Regex("open.spotify.com/track/[0-9a-zA-Z]+");
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
        
        public Track CreateSpotifyTrack(FullTrack track)
        {
            return new () {Source = new SpotifySource(track), Position = TimeSpan.Zero};
        }
        
        public Track[] CreateSpotifyAlbum(FullAlbum album)
        {
            List<Track> requests = new List<Track>();
            var tracks = album.Tracks.Items;
            int cnt = 0;
            foreach (var track in tracks)
            {
                requests.Add(new () {Source = new SpotifySource(track), Position = TimeSpan.Zero});
                cnt++;
                if (cnt >= 500)
                {
                    break;
                }
            }
            return requests.ToArray();
        }
        
        public Track[] CreateSpotifyPlaylist(FullPlaylist  album)
        {
            List<Track> requests = new List<Track>();
            var tracks = album.Tracks.Items;
            int cnt = 0;
            foreach (var ptrack in tracks)
            {
                if (ptrack.Track is FullTrack track)
                {
                    requests.Add(new () {Source = new SpotifySource(track), Position = TimeSpan.Zero});
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