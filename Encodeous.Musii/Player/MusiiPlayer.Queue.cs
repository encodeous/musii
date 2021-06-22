using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Encodeous.Musii.Network;
using Humanizer;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Queue Methods

        public async Task SendQueueMessage(int startPage = 1)
        {
            var msg = await Text.SendMessageAsync(BuildQueueEmbed(startPage - 1, await GetQueue()));
            _queueMessage = msg;
            Task.Run(async () =>
            {
                DateTime startTime = DateTime.UtcNow;
                var l = DiscordEmoji.FromName(Client, ":arrow_left:");
                var r = DiscordEmoji.FromName(Client, ":arrow_right:");
                var refresh = DiscordEmoji.FromName(Client, ":arrows_counterclockwise:");
                int curpg = startPage - 1;

                await msg.CreateReactionAsync(l);
                await msg.CreateReactionAsync(refresh);
                await msg.CreateReactionAsync(r);
                
                bool changed = false;
                while (!_stopped && msg == _queueMessage && DateTime.Now - startTime <= TimeSpan.FromMinutes(1))
                {
                    var cq = await GetQueue();
                    int tpages = (int) Math.Ceiling((cq.Count - 1) / 20.0);
                    if (curpg >= tpages) curpg = tpages - 1;
                    if (curpg < 0) curpg = 0;
                    if (changed)
                    {
                        startTime = DateTime.UtcNow;
                        await msg.ModifyAsync(async x => x.Embed = BuildQueueEmbed(curpg, cq));
                    }
                    changed = false;
                    var res = await msg.CollectReactionsAsync(TimeSpan.FromMilliseconds(500));
                    if (res.Any(x => x.Emoji == l))
                    {
                        var x = res.First(x => x.Emoji == l);
                        foreach (var u in x.Users)
                        {
                            try
                            {
                                if (u != Client.CurrentUser)
                                {
                                    await msg.DeleteReactionAsync(l, u);
                                    changed = true;
                                    curpg--;
                                    if (curpg < 0)
                                    {
                                        curpg = tpages - 1;
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                    } 
                    else if (res.Any(x => x.Emoji == r))
                    {
                        var x = res.First(x => x.Emoji == r);
                        foreach (var u in x.Users)
                        {
                            try
                            {
                                if (u != Client.CurrentUser)
                                {
                                    await msg.DeleteReactionAsync(r, u);
                                    changed = true;
                                    curpg++;
                                    if (curpg > tpages - 1)
                                    {
                                        curpg = 0;
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                    else if(res.Any(x => x.Emoji == refresh))
                    {
                        var x = res.First(x => x.Emoji == refresh);
                        foreach (var u in x.Users)
                        {
                            try
                            {
                                if (u != Client.CurrentUser)
                                {
                                    await msg.DeleteReactionAsync(refresh, u);
                                    changed = true;
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            });
        }

        private DiscordEmbedBuilder BuildQueueEmbed(int page, List<IMusicSource> q, int itemsPerPage = 20)
        {
            // 20 items per pag
            int tpages = (int) Math.Ceiling((q.Count - 1) / (double)itemsPerPage);
            tpages = Math.Max(tpages, 1);
            if (page < 0) page = 0;
            if (page >= tpages) page = tpages - 1;
            var sel = q.GetRange(page * itemsPerPage + 1, Math.Min(20, q.Count - page * itemsPerPage - 1));
            var builder = new DiscordEmbedBuilder()
                .WithTitle($"Queue for {Voice.Name} - Page {page + 1}/{tpages}");

            if (sel.Count >= 1)
            {
                var selTrack = State.CurrentTrack;
                builder.AddField("Now playing",
                    $"`{selTrack.Title}`\n{Utils.GetProgress(selTrack.Position / selTrack.Length)}\n" +
                    $"**{selTrack.Position.MusiiFormat()} / {selTrack.Length.MusiiFormat()}**");
            }
            else
            {
                builder.WithDescription("Queue is empty.");
            }

            if (sel.Count > 1)
            {
                builder.AddField($"In queue - `{q.Count - 1}`",
                    string.Join("\n", sel
                        .Select((x, i) => $"`{i + page * itemsPerPage + 1}: {x.GetTrackName().Truncate(90,"...")}`")));
            }

            string footer = $"In channel {Voice.Name} - {State.Volume}% Volume - Started {(DateTime.UtcNow - State.StartTime).Humanize(2)} ago";
            if (State.IsLooped)
            {
                footer += " - On Loop";
            }

            if (State.IsPaused)
            {
                footer += " - Paused";
            }
            
            if (State.IsLocked)
            {
                footer += " - Locked";
            }

            builder.WithFooter(footer);
            return builder;
        }

        public Task<List<IMusicSource>> GetQueue()
        {
            return this.ExecuteSynchronized(() =>
            {
                var lst = new List<IMusicSource>();
                if (State.CurrentTrack is not null)
                {
                    lst.Add(new YoutubeSource(State.CurrentTrack));
                }

                lst.AddRange(State.Tracks);
                return Task.FromResult(lst);
            });
        }

        #endregion
    }
}