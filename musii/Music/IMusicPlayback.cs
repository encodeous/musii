using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace musii.Music
{
    interface IMusicPlayback
    {
        public Stream GetStream();

        public void Stop();

        public string Name { get; }

        public string PlaybackId { get; }

        public string ImageUrl { get; }

        public TimeSpan Duration { get; }

        public TimeSpan PlayTime { get; }

        public bool IsSkipped { get; set; }

        public bool ShowSkipMessage { get; set; }
    }
}
