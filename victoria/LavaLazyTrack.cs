using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace Victoria
{
    public class LavaLazyTrack : ILavaTrack
    {
        public string Author { get{if(!_loaded)LoadData();return _author;}}
        public bool CanSeek { get { if (!_loaded) LoadData(); return _canSeek; } }
        public TimeSpan Duration { get { if (!_loaded) LoadData(); return _duration; } }
        public string Hash { get { if (!_loaded) LoadData(); return _hash; } }
        public string Id { get { if (!_loaded) LoadData(); return _id; } }
        public bool IsStream { get { if (!_loaded) LoadData(); return _isStream; } }
        public TimeSpan Position { get { if (!_loaded) LoadData(); return _position; }
            set { if (!_loaded) LoadData(); _position = value; }
        }
        public string Title => OriginalTitle;
        public string Url { get { if (!_loaded) LoadData(); return _url; } }

        private string _author;
        private bool _canSeek;
        private TimeSpan _duration;
        private string _hash;
        private string _id;
        private bool _isStream;
        private TimeSpan _position;
        public string OriginalTitle;
        private string _url;

        private bool _loaded;
        private readonly LavaNode _node;

        public LavaLazyTrack(string query, LavaNode queryNode)
        {
            LazyQueryString = query;
            _node = queryNode;
        }
        public string LazyQueryString { get; }

        private void LoadData()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                var response = _node.SearchAsync(LazyQueryString).Result.Tracks[0];
                _author = response.Author;
                _canSeek = response.CanSeek;
                _duration = response.Duration;
                _hash = response.Hash;
                _id = response.Id;
                _isStream = response.IsStream;
                _position = response.Position;
                OriginalTitle = response.Title;
                _url = response.Url;
            }
            catch
            {
                _hash = null;
            }
        }

    }
}
