using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode.Videos;

namespace musii.Music
{
    class MusicRequest
    {
        public Video RequestedVideo;

        public VideoId VideoId;

        public ulong RequestedBy;

        public DateTime StartedPlayingTime;

        public bool Equals(MusicRequest b1, MusicRequest b2)
        {
            if (b2 == null && b1 == null)
                return true;
            else if (b1 == null || b2 == null)
                return false;
            return b1.VideoId == b2.VideoId;
        }

        public int GetHashCode(MusicRequest obj)
        {
            return obj.VideoId.GetHashCode();
        }
    }
}
