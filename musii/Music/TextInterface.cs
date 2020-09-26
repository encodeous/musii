using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using musii.Utilities;
using Victoria;

namespace musii.Music
{
    static class TextInterface
    {
        public static string GetProgress(double percent)
        {
            int bars = (int)Math.Floor(percent * 20.0);
            string k = "**";
            for (int i = 0; i < bars; i++)
            {
                k += "═";
            }

            k += "⬤**";

            for (int i = 0; i < (20 - bars); i++)
            {
                k += "═";
            }

            return k;
        }
        public static Embed HelpMessage()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = "Contribute to Musii on Github! | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "**Musii - Help**",
                Description =
                    $"**PLAYBACK COMMANDS**\n" +
                    $"**!play** — [`p, pl, listen, yt, youtube, spotify, sp`] <youtube-link/spotify-playlist/album/track> — *Plays the youtube/spotify link in your current voice channel*\n" +
                    $"**!skip** — [`s`] <number-of-songs> — *Skips songs (1 by default)*\n" +
                    $"**!clear** — [`leave, empty, c, stop`] — *Clears the playback queue*\n" +
                    $"**!queue** — [`q, next`] — *Shows the songs in the queue*\n" +
                    $"**!loop** — [`l, lp, repeat`] — *Toggles playback loop*\n" +
                    $"**!shuffle** — [`r, random, mix`] — *Shuffle the playlist*\n" +
                    $"\n" +
                    $"**OTHER COMMANDS**\n" +
                    $"**!lock** — *Prevents people without Manage Messages permission from using music commands*\n" +
                    $"**!musii** — *Invite Musii to your server!*\n" +
                    $"**!help** — *Shows help information*\n" +
                    $"**!info** — *Displays runtime information*\n",
                Footer = footer
            }.Build();
        }

        public static Embed NowPlayingMessage(ILavaTrack playback, IVoiceChannel channel)
        {
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Now Playing `{playback.Title}`.",
                Description =
                    $"Playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`.",
                ThumbnailUrl = playback.ThumbnailUrl
            }.Build();
        }

        public static Embed FailedPlayingMessage(ILavaTrack playback, IVoiceChannel channel,  Exception e)
        {
            return new EmbedBuilder()
            {
                Color = Color.Orange,
                Title = $"Failed Playing `{playback.Title}`.",
                Description =
                    $"Failed playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`. \n{e.Message.Replace("\n"," ").Replace("\r","")}",
                ThumbnailUrl = playback.ThumbnailUrl
            }.Build();
        }

        public static Embed QueuedSongMessage(ILavaTrack playback, LavaPlayer player)
        {
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Queued Song `{playback.Title}`.",
                Description = $"`{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`.]\n" +
                              $"The queue now has `{player.Queue.Count}` songs.", 
                ThumbnailUrl = playback.ThumbnailUrl,
            }.Build();
        }

        public static Embed QueuedSongsMessage(int count)
        {
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Queued {count} songs."
            }.Build();
        }

        public static Embed NotFoundMessage(string query)
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Contribute to Musii | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"The query `{query}` was not found.",
                Description =
                    $"The resource was not available or is restricted. Double check if the resource exists or if it is age/region locked.",
                Footer = footer
            }.Build();
        }

        public static Embed QueueClearedMessage(int count)
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"The bot will leave the channel"
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"The queue was cleared.",
                Description =
                    $"`{count}` songs were removed. The playlist is now empty.",
                Footer = footer
            }.Build();
        }
        public static Embed LoopOn()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Loop Is Now Enabled"
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"The queue will now play on repeat",
                Description =
                    $"Toggled Loop On.",
                Footer = footer
            }.Build();
        }
        public static Embed LoopOff()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Loop Is Now Disabled"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"The queue will no longer play on repeat",
                Description =
                    $"Toggled Loop Off.",
                Footer = footer
            }.Build();
        }
        public static Embed LockOn()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"The session is now locked."
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Toggled Lock",
                Description =
                    $"Lock has been enabled, only users with Manage Messages permission will be able to interact with the playlist.",
                Footer = footer
            }.Build();
        }
        public static Embed LockOff()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"The session is now unlocked."
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"Toggled Lock",
                Description =
                    $"Lock has been disabled, any user will be able to interact with the playlist.",
                Footer = footer
            }.Build();
        }
        public static Embed Locked()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Lacking Manage Messages permission."
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"This session is locked to moderators.",
                Description =
                    $"You do not have access to this command, please contact a moderator if you believe this is a mistake.",
                Footer = footer
            }.Build();
        }
        public static Embed Shuffled()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Shuffle"
            };
            return new EmbedBuilder()
            {
                Color = Color.Teal,
                Title = $"The playlist has been randomized",
                Description =
                    $"The items in the playlist have been shuffled.",
                Footer = footer
            }.Build();
        }
        public static Embed NoMusic()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Music Player"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"There is no music playing.",
                Description =
                    $"Run this command when there is music playing.",
                Footer = footer
            }.Build();
        }
        public static Embed LockedPermission()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Lacking Manage Messages permission."
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"No Permissions",
                Description =
                    $"You do not have access to this command, please contact a moderator if you believe this is a mistake.",
                Footer = footer
            }.Build();
        }
        public static Embed SkipSongsMessage(int count)
        {
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Skipped {count} songs."
            }.Build();
        }

        public static Embed SkipSongMessage(ILavaTrack playback, IVoiceChannel channel)
        {
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Skipped Playing {playback.Title}",
                Description =
                    $"Skipped playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`.",
            }.Build();
        }

        public static Embed StoppedSongMessage(ILavaTrack playback, IVoiceChannel channel)
        {
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"Stopped Playing {playback.Title}",
                Description =
                    $"Stopped playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`.",
            }.Build();
        }

        public static Embed RequeuedSongMessage(ILavaTrack playback, IVoiceChannel channel)
        {
            return new EmbedBuilder()
            {
                Color = Color.DarkOrange,
                Title = $"Stopped Playing {playback.Title}",
                Description =
                    $"Stopped playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`. The song will be requeued. (Loop is on)",
            }.Build();
        }

        public static Embed NetworkErrorMessage(ILavaTrack playback, IVoiceChannel channel)
        {
            return new EmbedBuilder()
            {
                Color = Color.Purple,
                Title = $"Encountered Network Error while playing {playback.Title}",
                Description =
                    $"Skipped playing song `{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`] in `{channel.Name}`.",
            }.Build();
        }

        public static Embed GetQueueMessage(LavaPlayer music, bool looped)
        {
            int min = Math.Min(20, music.Queue.Count);
            StringBuilder sb = new StringBuilder();
            var l = music.Queue;
            int cnt = 0;

            var cur = music.Track;
            if (cur == null)
            {
                sb.Append($"**The queue is empty**\n");
            }
            else
            {
                sb.Append($"**Now Playing:** `{cur.Title}`\n");
                sb.Append($"{GetProgress(cur.Position / cur.Duration)}\n");
                sb.Append($"**{MessageSender.TimeSpanFormat(cur.Position)} / {MessageSender.TimeSpanFormat(cur.Duration)}**\n");
                sb.Append($"\n**In Queue:**\n");
            }

            var imn = l.GetEnumerator();
            for (int i = 0; i < min; i++)
            {
                cnt++;
                sb.Append($"**{cnt}**: `{imn.Current.Title}`\n");
                imn.MoveNext();
            }

            var footer = new EmbedFooterBuilder()
            {
                Text = (min == music.Queue.Count) ? "End of playlist." : $"Total {music.Queue.Count} songs."
            };

            return new EmbedBuilder
            {
                Title = looped? $"Items In Queue (On Loop):" : $"Items In Queue:",
                Color = Color.Blue,
                Footer = footer,
                Description = sb.ToString()
            }.Build();
        }
    }
}
