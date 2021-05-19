﻿using System;
using System.Threading.Tasks;
using Encodeous.Musii.Data;
using Encodeous.Musii.Player;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Network
{
    public class SearchService
    {
        private SpotifyService _spotify;
        private YoutubeService _youtube;
        private PlayerSessions _sessions;

        public SearchService(SpotifyService spotify, YoutubeService youtube)
        {
            _spotify = spotify;
            _youtube = youtube;
        }

        public async Task<(Track[], string)> ParseGeneralAsync(string query)
        {
            string[] keywords = query.Split(" ");
            if (IsPlaylist(keywords))
            {
                try
                {
                    return (await _youtube.SearchPlaylist(keywords[0]), "");
                }
                catch
                {
                    return (null, $"The YouTube playlist `{keywords[0]}` was not found.");
                }
            }
            else if (IsVideo(keywords))
            {
                try
                {
                    return (new []{_youtube.SearchVideo(keywords[0])}, "");
                }
                catch
                {
                    return (null, $"The YouTube video `{keywords[0]}` was not found.");
                }
            }
            else if (_spotify.ParsePlaylist(keywords[0]) != "")
            {
                var playlist = await _spotify.GetPlaylist(keywords[0]);
                if (playlist is null)
                {
                    return (null, $"The Spotify playlist `{keywords[0]}` was not found.");
                }
                try
                {
                    return (_spotify.CreateSpotifyPlaylist(playlist), "");
                }
                catch
                {
                    return (null, $"Failed to get Spotify playlist `{keywords[0]}`.");
                }
            }
            else if (_spotify.ParseAlbum(keywords[0]) != "")
            {
                var album = await _spotify.GetAlbum(keywords[0]);
                if (album is null)
                {
                    return (null, $"The Spotify album `{keywords[0]}` was not found.");
                }
                try
                {
                    return (_spotify.CreateSpotifyAlbum(album), "");
                }
                catch
                {
                    return (null, $"Failed to get Spotify album `{keywords[0]}`.");
                }
            }
            else if (_spotify.ParseTrack(keywords[0]) != "")
            {
                var track = await _spotify.GetTrack(keywords[0]);
                if (track is null)
                {
                    return (null, $"The Spotify track `{keywords[0]}` was not found.");
                }
                try
                {
                    return (new []{_spotify.CreateSpotifyTrack(track)}, "");
                }
                catch
                {
                    return (null, $"Failed to get Spotify track `{keywords[0]}`.");
                }
            }
            else if (Guid.TryParse(keywords[0], out var x))
            {
                var state = _sessions.CopyState(x);
                if (state is null)
                {
                    return (null, "The playlist id was not found.");
                }

                return (state.Tracks.ToArray(), "");
            }
            else
            {
                try
                {
                    return (new []{await _youtube.SearchVideo(keywords)}, "");
                }
                catch
                {
                    return (null, $"The search query `{string.Join(" ",keywords)}` was not found.");
                }
            }
        }

        #region Utils

        public bool IsPlaylist(string[] keywords)
        {
            return IsUrl(keywords[0]) && PlaylistId.TryParse(keywords[0]).HasValue;
        }
        public bool IsVideo(string[] keywords)
        {
            return IsUrl(keywords[0]) && VideoId.TryParse(keywords[0]).HasValue;
        }
        private bool IsUrl(string s)
        {
            return Uri.TryCreate(s, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        #endregion
    }
}