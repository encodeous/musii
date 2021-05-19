using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Data
{
    public class YoutubeLazySource : IMusicSource
    {
        private string _title, _imageLink;
        private TimeSpan _duration;
        private VideoId _id;
        private bool _isStream = false;
        private bool _hasRefreshed = false;

        /// <summary>
        /// This constructor is meant for cloning
        /// </summary>
        public YoutubeLazySource()
        { }
        public YoutubeLazySource(VideoId video)
        {
            _id = video;
        }

        public async Task Refresh(HttpClient client)
        {
            if (_hasRefreshed) return;
            _hasRefreshed = true;
            var yt = new YoutubeClient(client);
            var video = await yt.Videos.GetAsync(_id);
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
            return new YoutubeLazySource()
            {
                _duration = _duration,
                _id = _id,
                _title = new string(_title),
                _imageLink = new string(_imageLink),
                _isStream = _isStream,
                _hasRefreshed = _hasRefreshed
            };
        }
    }
}