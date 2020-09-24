using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using musii.Music;
using musii.Utilities;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace musii.Music
{
    class CaptionService
    {
        public static async Task CaptionAsync(IMusicPlayback v, YoutubeClient client, ISocketMessageChannel channel)
        {
            string pid = "";
            if (v is YoutubePlayback) pid = v.PlaybackId;
            else pid = ((SpotifyPlayback) v).LoadFromYoutube().Id;
            var captionTracks =
                (await client.Videos.ClosedCaptions.GetManifestAsync(pid).ConfigureAwait(false)).Tracks;
            ClosedCaptionTrackInfo captionMeta = null;
            foreach (var track in captionTracks)
            {
                if (track.Language.Code.StartsWith("en"))
                {
                    captionMeta = track;
                    break;
                }
            }
            if (captionMeta == null)
            {
                return;
            }
            var captions = await client.Videos.ClosedCaptions.GetAsync(captionMeta).ConfigureAwait(false);

            var eb = new EmbedBuilder {Title = $"Closed Captions [`{captionMeta.Language.Name}`]"};
            eb.WithFooter(v.Name , v.ImageUrl);
            eb.WithColor(Color.Red);

            var msg = await channel.SendMessageAsync(null, false, eb.Build()).ConfigureAwait(false);
            
            DateTime start = DateTime.Now;

            try
            {
                foreach (var cap in captions.Captions)
                {
                    var st = cap.Offset;

                    if (DateTime.Now - start < st)
                    {
                        await Task.Delay(st - (DateTime.Now - start), v.SkipToken).ConfigureAwait(false);
                    }

                    eb.WithDescription($"**[{MessageSender.TimeSpanFormat(st)}]** `" + cap.Text + "`");
                    await msg.ModifyAsync(x => { x.Embed = eb.Build(); }).ConfigureAwait(false);

                    if (v.SkipToken.IsCancellationRequested)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                        return;
                    }
                }
            }
            catch
            {
                // ignored
            }
            await msg.DeleteAsync().ConfigureAwait(false);
        }
    }
}
