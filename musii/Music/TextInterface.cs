using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using musii.Utilities;
using Victoria;
using Victoria.Enums;

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
        public static Embed HelpMessage(string prefix)
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = "Contribute to Musii on Github! | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"**{Config.Name} - Help**",
                Description =
                    $"**PLAYBACK COMMANDS**\n" +
                    $"**{prefix}play** — [`p, pl, listen, yt, youtube, spotify, sp`] <youtube-link/spotify-playlist/album/track> — *Plays the youtube/spotify link in your current voice channel*\n" +
                    $"**{prefix}skip** — [`s`] <number-of-songs> — *Skips songs (1 by default)*\n" +
                    $"**{prefix}clear** — [`leave, empty, c, stop`] — *Clears the playback queue*\n" +
                    $"**{prefix}queue** — [`q, next`] — *Shows the songs in the queue*\n" +
                    $"**{prefix}loop** — [`l, lp, repeat`] — *Toggles playback loop*\n" +
                    $"**{prefix}shuffle** — [`r, random, mix`] — *Shuffle the playlist*\n" +
                    $"**{prefix}pause** — [`ps, hold, suspend`] — *Temporarily pause playback*\n" +
                    $"**{prefix}volume** — [`v, vol`] <volume> — *Adjust Playback Volume [0%-1000%]*\n" +
                    $"**{prefix}seek** — [`move, set, m`] <time-delta> <s/m/h> — *Skip forward or back on current playback*\n" +
                    $"\n" +
                    $"**OTHER COMMANDS**\n" +
                    $"**{prefix}lock** — *Prevents people without Manage Messages permission from modifying the session*\n" +
                    $"**{prefix}help** — *Shows help information*\n" +
                    $"**{prefix}info** — *Displays runtime information*\n" +
                    $"**{prefix}prefix** — <prefix> — *Set the prefix of the bot, requires modify message permissions.*",
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
            }.Build();
        }

        public static Embed QueuedSongMessage(ILavaTrack playback, LavaPlayer player, string thumbnailUrl)
        {
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Queued Song `{playback.Title}`.",
                Description = $"`{playback.Title}` [`{MessageSender.TimeSpanFormat(playback.Duration)}`.]\n" +
                              $"The queue now has `{player.Queue.Count+1}` songs.", 
                ThumbnailUrl = thumbnailUrl,
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
        public static Embed InternalErrorMessage(string exception)
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Contribute to Musii | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"An Internal Error Occurred.",
                Description =
                    $"Exception: {exception}",
                Footer = footer
            }.Build();
        }

        public static Embed StandardMessage(string s)
        {
            return new EmbedBuilder()
            {
                Color = Color.DarkTeal,
                Description = s
            }.Build();
        }

        public static Embed ReconnectingMessage()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Contribute to Musii | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.DarkTeal,
                Title = $"Connection has been lost, reconnecting.",
                Description =
                    $"Discord has been unstable lately, the bot has been disconnected. Music Playback will resume in a few moments.",
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
        public static Embed FinishedPlayingMessage()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"The bot will leave the channel"
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"The Playlist is empty",
                Description =
                    $"There are no more songs to play, you can turn on loop for constant playback.",
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
        public static Embed Authorized()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Successfully Authorized Guild"
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Authorized",
                Description =
                    $"This guild has been authorized to use {Config.Name}. Type !help for help.",
                Footer = footer
            }.Build();
        }
        public static Embed Unauthorized()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Please use !authorize to authorize the use of {Config.Name}"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"This guild has not been authorized to use {Config.Name}.",
                Description =
                    $"Please contact the owner of this bot to authorize the use of {Config.Name} in this server.",
                Footer = footer
            }.Build();
        }
        public static Embed PauseOn()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Playback is now paused"
            };
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Paused",
                Description =
                    $"The music will stop playing.",
                Footer = footer
            }.Build();
        }
        public static Embed PauseOff()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = $"Playback is nolonger paused"
            };
            return new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"Resumed",
                Description =
                    $"The music will start playing.",
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
        public static Embed SeekMessage(ILavaTrack cur, TimeSpan seekTime, bool paused)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{GetProgress(seekTime / cur.Duration)}\n");
            sb.Append($"**{MessageSender.TimeSpanFormat(seekTime)} / {MessageSender.TimeSpanFormat(cur.Duration)}**  {(paused ? "- **PAUSED**" : "")}\n");
            return new EmbedBuilder()
            {
                Color = Color.Green,
                Title = $"Playback Time Set",
                Description = sb.ToString()
            }.Build();
        }

        public static Embed GetQueueMessage(LavaPlayer music)
        {
            int min = Math.Min(20, music.Queue.Count);
            StringBuilder sb = new StringBuilder();
            var l = music.Queue;

            var cur = music.Track;
            sb.Append($"\n**In Queue:**\n");
            var imn = l.InternalList.Take(min);
            imn = imn.Reverse();
            int cnt = min;
            foreach (var k in imn)
            {
                sb.Append($"**{cnt}**: `{k.Title}`\n");
                cnt--;
            }

            sb.Append("\n");

            sb.Append($"**Now Playing:** `{cur.Title}`\n");
            sb.Append($"{GetProgress(cur.Position / cur.Duration)}\n");
            sb.Append($"**{MessageSender.TimeSpanFormat(cur.Position)} / {MessageSender.TimeSpanFormat(cur.Duration)}**  {((music.PlayerState == PlayerState.Paused) ? "- **PAUSED**" : "")}\n");

            var footer = new EmbedFooterBuilder()
            {
                Text = (min == music.Queue.Count) ? "Entire Playlist Shown." : $"Total {music.Queue.Count} songs."
            };

            return new EmbedBuilder
            {
                Title = music.Looped? $"Items In Queue (On Loop):" : $"Items In Queue:",
                Color = Color.Blue,
                Footer = footer,
                Description = sb.ToString()
            }.Build();
        }
    }
}
