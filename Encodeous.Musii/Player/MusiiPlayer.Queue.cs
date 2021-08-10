using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Humanizer;

namespace Encodeous.Musii.Player
{
    public partial class MusiiPlayer
    {
        #region Queue Methods

        public async Task SendQueueMessageAsync(int startPage = 1)
        {
            var msg = await Text.SendMessageAsync(await BuildQueueEmbedAsync(startPage - 1, await GetQueueAsync(),
                _queueLength));
            _queueMessage = msg;
            Task.Run(async () =>
            {
                try
                {
                    DateTime startTime = DateTime.UtcNow;
                    var l = DiscordEmoji.FromName(_client, ":arrow_left:");
                    var r = DiscordEmoji.FromName(_client, ":arrow_right:");
                    var refresh = DiscordEmoji.FromName(_client, ":arrows_counterclockwise:");
                    int curpg = startPage - 1;

                    await msg.CreateReactionAsync(l);
                    await msg.CreateReactionAsync(refresh);
                    await msg.CreateReactionAsync(r);

                    bool changed = false;
                    while (!_stopped && msg == _queueMessage && DateTime.Now - startTime <= _queueTimeout)
                    {
                        var cq = await GetQueueAsync();
                        int tpages = (int) Math.Ceiling((cq.Count - 1) / 20.0);
                        if (curpg >= tpages) curpg = tpages - 1;
                        if (curpg < 0) curpg = 0;
                        if (changed)
                        {
                            startTime = DateTime.UtcNow;
                            var embed = await BuildQueueEmbedAsync(curpg, cq);
                            await msg.ModifyAsync(x => x.Embed = embed);
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
                                    if (u != _client.CurrentUser)
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
                                    if (u != _client.CurrentUser)
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
                        else if (res.Any(x => x.Emoji == refresh))
                        {
                            var x = res.First(x => x.Emoji == refresh);
                            foreach (var u in x.Users)
                            {
                                try
                                {
                                    if (u != _client.CurrentUser)
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
                }
                catch
                {
                    _queueMessage = null;
                }
            });
        }

        private async Task<DiscordEmbedBuilder> BuildQueueEmbedAsync(int page, List<BaseMusicSource> q, int itemsPerPage = 20)
        {
            // 20 items per pag
            int tpages = (int) Math.Ceiling((q.Count - 1) / (double)itemsPerPage);
            tpages = Math.Max(tpages, 1);
            if (page < 0) page = 0;
            if (page >= tpages) page = tpages - 1;
            var sel = q.GetRange(page * itemsPerPage + 1, Math.Min(20, q.Count - page * itemsPerPage - 1));
            var builder = new DiscordEmbedBuilder()
                .WithTitle($"Queue for {Voice.Name} - Page {page + 1}/{tpages}");

            var selTrack = await _guild.ResolveTrackAsync(State.CurrentTrack);
            builder.AddField("Now playing",
                $"`{selTrack.Title}`\n{Utils.GetProgress(State.CurrentPosition / selTrack.Length)}\n" +
                $"**{State.CurrentPosition.MusiiFormat()} / {selTrack.Length.MusiiFormat()}**");
            if(sel.Count == 0)
            {
                builder.WithDescription("Queue is empty.");
            }

            if (sel.Count >= 1)
            {
                builder.AddField($"In queue - `{q.Count - 1}`",
                    string.Join("\n", sel
                        .Select((x, i) => $"`{i + page * itemsPerPage + 1}: {x.GetTrackName().Truncate(90,"...")}`")));
            }

            string footer = $"In channel {Voice.Name} - {State.Volume}% Volume - Started {(DateTime.UtcNow - State.StartTime).Humanize(2)} ago";
            if (State.Loop == LoopType.Playlist)
            {
                footer += " - Playlist on loop";
            }
            if (State.Loop == LoopType.Song)
            {
                footer += " - Song on loop";
            }

            if (State.Filter != AudioFilter.None)
            {
                footer += $" - {State.Filter} Filter";
            }

            if (State.IsPaused)
            {
                footer += " - Paused";
            }
            
            if (State.IsLocked)
            {
                footer += " - Locked";
            }
            
            if (State.IsPinned)
            {
                footer += " - Pinned";
            }

            builder.WithFooter(footer);
            builder.WithThumbnail(selTrack.GetThumbnail());
            return builder;
        }

        public Task<List<BaseMusicSource>> GetQueueAsync()
        {
            return ExecuteSynchronized(() =>
            {
                var lst = new List<BaseMusicSource>();
                if (State.CurrentTrack is not null)
                {
                    lst.Add(State.CurrentTrack.Clone());
                }

                lst.AddRange(State.Tracks);
                return Task.FromResult(lst);
            });
        }

        #endregion
    }
}