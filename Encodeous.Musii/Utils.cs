using System.Globalization;
using System.Reflection;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Encodeous.Musii
{
    public static class Utils
    {
        public static LavalinkTrack CloneTrack(this LavalinkTrack track)
        {
            // dirty way to make a clone
            var json = JsonConvert.SerializeObject(track);
            var obj = JsonConvert.DeserializeObject<LavalinkTrack>(json);
            obj.TrackString = track.TrackString;
            obj.SetPos((long)track.Position.TotalMilliseconds);
            return obj;
        }
        
        public static void SetPos(this LavalinkTrack track, long position)
        {
            typeof(LavalinkTrack).GetField("_position", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(track, position);
        }
        
        public static string GetThumbnail(this LavalinkTrack track)
        {
            return $"https://img.youtube.com/vi/{track.Identifier}/mqdefault.jpg";
        }
    }
}