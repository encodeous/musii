using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode.Videos;

namespace musii.Utilities
{
    class VideoEqualityComparator : IEqualityComparer<Video>
    {
        public bool Equals(Video b1, Video b2)
        {
            if (b2 == null && b1 == null)
                return true;
            else if (b1 == null || b2 == null)
                return false;
            return b1.Id == b2.Id;
        }

        public int GetHashCode(Video obj)
        {
            return obj.GetHashCode();
        }
    }
}
