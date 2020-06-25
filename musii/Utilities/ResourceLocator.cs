using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace musii.Utilities
{
    class ResourceLocator
    {
        public static bool IsPlaylist(string[] keywords)
        {
            return IsUrl(keywords[0]) && PlaylistId.TryParse(keywords[0]).HasValue;
        }
        public static bool IsVideo(string[] keywords)
        {
            return IsUrl(keywords[0]) && VideoId.TryParse(keywords[0]).HasValue;
        }
        private static bool IsUrl(string s)
        {
            return Uri.TryCreate(s, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

}
