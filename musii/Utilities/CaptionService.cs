using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using musii.Music;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace musii.Utilities
{
    class CaptionService
    {
        public static async Task CaptionAsync(CancellationToken cancellationToken, MusicRequest v, YoutubeClient client, ISocketMessageChannel channel)
        {
            var captionTracks =
                (await client.Videos.ClosedCaptions.GetManifestAsync(v.VideoId).ConfigureAwait(false)).Tracks;
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
            eb.WithFooter(v.RequestedVideo.Title , v.RequestedVideo.Thumbnails.LowResUrl);
            eb.WithColor(Color.Red);

            var msg = await channel.SendMessageAsync(null, false, eb.Build()).ConfigureAwait(false);
            
            DateTime start = DateTime.Now;

            try
            {
                foreach (var cap in captions.Captions)
                {
                    if (cap != null)
                    {
                        var st = cap.Offset;

                        if (DateTime.Now - start < st)
                        {
                            await Task.Delay(st - (DateTime.Now - start), cancellationToken).ConfigureAwait(false);
                        }

                        eb.WithDescription($"**[{MessageSender.TimeSpanFormat(st)}]** `" + cap.Text + "`");
                        await msg.ModifyAsync(x => { x.Embed = eb.Build(); }).ConfigureAwait(false);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                            return;
                        }
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
