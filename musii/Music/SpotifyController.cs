using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace musii.Music
{
    class SpotifyController
    {
        public static SpotifyClient Client;
        public static void InitializeSpotify()
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(Config.SpotifyClientId, Config.SpotifyClientSecret));

            Client =  new SpotifyClient(config);
            "Logged in with API".Log();
        }

        public static string ParsePlaylist(string url)
        {
            var rx = new Regex("open.spotify.com/playlist/[0-9a-zA-Z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(26);
        }

        public static string ParseAlbum(string url)
        {
            var rx = new Regex("open.spotify.com/album/[0-9a-zA-Z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(23);
        }

        public static string ParseTrack(string url)
        {
            var rx = new Regex("open.spotify.com/track/[0-9a-zA-Z]+");
            if (!rx.Match(url).Success) return "";
            return rx.Match(url).Value.Substring(23);
        }

        public static async Task<FullPlaylist> GetPlaylist(string url)
        {
            string id = ParsePlaylist(url);
            if (id == "") return null;
            try
            {
                return await Client.Playlists.Get(id);
            }
            catch
            {

            }
            return null;
        }
        public static async Task<FullAlbum> GetAlbum(string url)
        {
            string id = ParseAlbum(url);
            if (id == "") return null;
            try
            {
                return await Client.Albums.Get(id);
            }
            catch
            {

            }
            return null;
        }
        public static async Task<FullTrack> GetTrack(string url)
        {
            string id = ParseTrack(url);
            if (id == "") return null;
            try
            {
                return await Client.Tracks.Get(id);
            }
            catch
            {

            }
            return null;
        }
    }
}
