using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Player;
using Newtonsoft.Json;
using SpotifyAPI.Web;

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
        
        private static Random rng = new Random();  

        public static void Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }
        
        public static void SetPos(this LavalinkTrack track, long miliseconds)
        {
            typeof(LavalinkTrack).GetField("_position", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(track, miliseconds);
        }
        
        public static string GetThumbnail(this LavalinkTrack track)
        {
            return $"https://img.youtube.com/vi/{track.Identifier}/mqdefault.jpg";
        }

        public static bool IsExecutedByBotOwner(this CommandContext ctx)
        {
            var oAttr = new RequireOwnerAttribute();
            try
            {
                return oAttr.ExecuteCheckAsync(ctx, true).GetAwaiter().GetResult();
            }
            catch
            {
                
            }

            return false;
        }
        public static bool HasPermission(this CommandContext ctx, Permissions perms)
        {
            var oAttr = new RequireUserPermissionsAttribute(perms);
            try
            {
                return oAttr.ExecuteCheckAsync(ctx, true).GetAwaiter().GetResult();
            }
            catch
            {
                
            }

            return false;
        }
        public static string GetProgress(double percent)
        {
            int bars = (int)Math.Floor(percent * 40.0);
            string k = "**";
            for (int i = 0; i < bars; i++)
            {
                k += "═";
            }

            k += "⬤**";

            for (int i = 0; i < (40 - bars); i++)
            {
                k += "═";
            }

            return k;
        }
        public static string MusiiFormat(this TimeSpan timeSpan)
        {
            return (Math.Floor(timeSpan.TotalHours).ToString("00")) + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00");
        }
        
        public static async Task<T> ExecuteSynchronized<T>(this NewMusicPlayer player, Func<Task<T>> func, bool qUpdate = false)
        {
            await player.State.StateLock.WaitAsync();
            try
            {
                return await func();
            }
            finally
            {
                player.State.StateLock.Release();
                if (qUpdate)
                {
                    player.State.QueueUpdate.Set();
                    player.State.QueueUpdate.Reset();
                }
            }
        }
        public static async Task ExecuteSynchronized(this NewMusicPlayer player, Func<Task> func, bool qUpdate = false)
        {
            await player.State.StateLock.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                player.State.StateLock.Release();
                if (qUpdate)
                {
                    player.State.QueueUpdate.Set();
                    player.State.QueueUpdate.Reset();
                }
            }
        }
    }
}