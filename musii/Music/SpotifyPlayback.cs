using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using musii.Utilities;
using SpotifyAPI.Web;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace musii.Music
{
    class SpotifyPlayback : IMusicPlayback
    {
        private static YoutubeClient _client = new YoutubeClient();
        private Video _youtubeVideo;
        private Process _mpeg;
        private Stream _youtubeStream;
        private DateTime _startTime = DateTime.MinValue;

        private string trackQuery = "";
        public SpotifyPlayback(SimpleTrack track)
        {
            PlaybackId = track.Id;
            _name = track.Name;
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }

            trackQuery = query;
        }
        public SpotifyPlayback(FullTrack track)
        {
            PlaybackId = track.Id;
            _name = track.Name;
            var query = track.Name + " ";
            foreach (var a in track.Artists)
            {
                query += a.Name + " ";
            }

            trackQuery = query;
        }

        public Video LoadFromYoutube()
        {
            return _client.Search.GetVideosAsync(trackQuery).FirstAsync().Result;
        }

        public Stream GetStream()
        {
            if (_youtubeVideo == null) UpdateDetails();

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

        private string _imageUrl;

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
        public string Name => _name;

        private string _name;
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
        public string PlaybackId { get; }
        public TimeSpan PlayTime => DateTime.UtcNow - _startTime;
        public bool IsSkipped { get; set; }
        public bool ShowSkipMessage { get; set; }

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private void UpdateDetails()
        {
            semaphore.Wait();
            if (_youtubeVideo != null)
            {
                semaphore.Release();
                return;
            }

            _youtubeVideo = LoadFromYoutube();
            _imageUrl = _youtubeVideo.Thumbnails.MediumResUrl;
            _length = _youtubeVideo.Duration;
            _name = _youtubeVideo.Title;
            semaphore.Release();
        }
    }
}
