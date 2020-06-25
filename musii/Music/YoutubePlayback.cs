using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using musii.Utilities;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace musii.Music
{
    class YoutubePlayback : IMusicPlayback
    {
        private static YoutubeClient _client = new YoutubeClient();
        private Video _youtubeVideo;
        private Process _mpeg;
        private Stream _youtubeStream;
        private DateTime _startTime;
        public string PlaybackId { get; }
        public TimeSpan PlayTime => DateTime.UtcNow - _startTime;

        public bool IsSkipped { get; set; }
        public bool ShowSkipMessage { get; set; }
        private string _imageUrl;
        private string _name;
        private TimeSpan _length = TimeSpan.Zero;
        public string ImageUrl
        {
            get
            {
                if (_imageUrl == null)
                {
                    UpdateDetails();
                }

                return _imageUrl;
            }
        }
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    UpdateDetails();
                }

                return _name;
            }
        }
        public TimeSpan Duration
        {
            get
            {
                if (_length == TimeSpan.Zero)
                {
                    UpdateDetails();
                }

                return _length;
            }
        }

        public static YoutubePlayback Parse(string id)
        {
            var vid = VideoId.TryParse(id);
            if (!vid.HasValue) return null;
            return new YoutubePlayback(vid.Value);
        }

        private YoutubePlayback(VideoId id)
        {
            PlaybackId = id.Value;
        }

        public YoutubePlayback(Video v)
        {
            PlaybackId = v.Id.Value;
            _youtubeVideo = v;
            _imageUrl = _youtubeVideo.Thumbnails.MediumResUrl;
            _length = _youtubeVideo.Duration;
            _name = _youtubeVideo.Title;
        }

        public Stream GetStream()
        {
            if(_youtubeVideo == null) UpdateDetails();

            if (_youtubeStream == null)
            {
                _startTime = DateTime.UtcNow;
                var manifest = _client.Videos.Streams.GetManifestAsync(_youtubeVideo.Id).Result;
                IStreamInfo streamInfo = manifest.GetAudioOnly().WithHighestBitrate();

                if (streamInfo == null) return null;

                _mpeg = Ffmpeg.CreateFfmpeg(streamInfo.Url);
                _youtubeStream = _mpeg.StandardOutput.BaseStream;
            }

            return _youtubeStream;
        }

        public void Stop()
        {
            try
            {
                _mpeg?.Kill();
            }
            catch
            {

            }

            try
            {
                _youtubeStream?.Dispose();
            }
            catch
            {

            }
        }
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private void UpdateDetails()
        {
            semaphore.Wait();
            if (_youtubeVideo != null)
            {
                semaphore.Release();
                return;
            }
            _youtubeVideo = _client.Videos.GetAsync(PlaybackId).Result;
            _imageUrl = _youtubeVideo.Thumbnails.MediumResUrl;
            _length = _youtubeVideo.Duration;
            _name = _youtubeVideo.Title;
            semaphore.Release();
        }
    }
}
