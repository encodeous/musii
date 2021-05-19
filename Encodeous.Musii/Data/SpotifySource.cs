using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Data
{
    public class SpotifySource : IMusicSource
    {
        private string _title, _imageLink;
        private TimeSpan _duration;
        private bool _hasRefreshed = false;
        private string _query;
        private bool _isStream = false;
        private VideoId _id;

        /// <summary>
        /// This constructor is meant for cloning
        /// </summary>
        public SpotifySource()
        { }
        public SpotifySource(FullTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            _query = query;
            _title = track.Name;
        }
        public SpotifySource(SimpleTrack track)
        {
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }
            _query = query;
            _title = track.Name;
        }

        public async Task Refresh(HttpClient client)
        {
            if (_hasRefreshed) return;
            _hasRefreshed = true;
            var yt = new YoutubeClient(client);
            var videos = yt.Search.GetVideosAsync(_query);
            await videos.GetAsyncEnumerator().MoveNextAsync();
            var enu = videos.GetAsyncEnumerator();
            await enu.MoveNextAsync();
            var video = enu.Current;
            _id = video.Id;
            _title = video.Title;
            _imageLink = video.Thumbnails[0].Url;
            if (video.Duration.HasValue && video.Duration.Value != TimeSpan.Zero)
            {
                _duration = video.Duration.Value;
            }
            else
            {
                _duration = TimeSpan.Zero;
                _isStream = true;
            }
        }

        public async Task<string> GetStreamUrl(HttpClient client)
        {
            var yt = new YoutubeClient(client);
            if (!_isStream)
            {
                var qvid = await yt.Videos.Streams.GetManifestAsync(_id);
                var streams = qvid.GetAudioOnlyStreams().Where(x=>x.AudioCodec == "opus");
                var hqstream = streams.OrderByDescending(x => x.Bitrate.BitsPerSecond).First();
                return hqstream.Url;
            }
            else
            {
                return await yt.Videos.Streams.GetHttpLiveStreamUrlAsync(_id);
            }
        }

        public string GetImageLink()
        {
            return _imageLink;
        }

        public TimeSpan GetDuration()
        {
            return _duration;
        }

        public string GetTitle()
        {
            return _title;
        }

        public bool IsStream()
        {
            return _isStream;
        }

        public IMusicSource CloneSource()
        {
            return new SpotifySource()
            {
                _duration = _duration,
                _query = _query,
                _id = _id,
                _title = new string(_title),
                _imageLink = new string(_imageLink),
                _isStream = _isStream,
                _hasRefreshed = _hasRefreshed
            };
        }
    }
}