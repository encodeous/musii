using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Data
{
    public class YoutubeSource : IMusicSource
    {
        protected string _title, _imageLink;
        protected TimeSpan _duration;
        protected VideoId _id;
        protected bool _isStream = false;

        /// <summary>
        /// This constructor is meant for cloning
        /// </summary>
        public YoutubeSource()
        { }
        public YoutubeSource(IVideo video)
        {
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

        public Task Refresh(HttpClient client)
        {
            return Task.CompletedTask;
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
            return new YoutubeSource()
            {
                _duration = _duration,
                _id = _id,
                _title = new string(_title),
                _imageLink = new string(_imageLink),
                _isStream = _isStream,
            };
        }
    }
}