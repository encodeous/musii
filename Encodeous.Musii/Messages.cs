﻿using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Core;
using Humanizer;
using Humanizer.Localisation;

namespace Encodeous.Musii
{
    public static class Messages
    {
        public static DiscordColor Success = DiscordColor.Green;
        public static DiscordColor Info = DiscordColor.Blurple;
        public static DiscordColor Warning = DiscordColor.Purple;
        public static DiscordColor Error = DiscordColor.Red;
        #region Player Messages
        public static DiscordMessageBuilder PlaylistEmptyMessage(this MusiiGuild data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playlist is empty.")
                    .WithColor(Warning)
                    .WithDescription("The bot will now leave the voice channel")
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder ActiveInOtherChannel(this MusiiGuild data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("The player is active in another text channel.")
                    .WithDescription($"The requested action has been performed in {data.Player.Text.Mention}"));
        }
        public static DiscordMessageBuilder NotAuthorized(ulong guildId)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("This guild has not yet been authorized to run Musii.")
                    .WithDescription($"Please ask the person operating this instance of Musii to authorize the current guild: (Guild Id: `{guildId}`). \n\nMake sure to copy the guild id fully, and send it to the operator")
                    .WithFooter("Musii is a privately hosted bot. (Operators can run the authorize <guild> command)"));
        }
        public static DiscordMessageBuilder AddedTrackMessage(this MusiiGuild data, LavalinkTrack track)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Song added to the queue")
                    .WithColor(Success)
                    .AddField(track.Title,
                        $"{track.Length.Humanize(4, minUnit: TimeUnit.Second)}", true)
                    .WithThumbnail(track.GetThumbnail())
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder ClearQueueMessage(this MusiiGuild data, int count)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Queue has been cleared")
                    .WithColor(Success)
                    .WithDescription($"{count} {(count == 1 ? "song":"songs")} have been removed"));
        }
        public static async Task<DiscordMessageBuilder> SkippedTrackMessageAsync(this MusiiGuild data)
        {
            var track = await data.ResolveTrackAsync(data.Player.State.CurrentTrack);
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Skipped Song")
                    .WithColor(Success)
                    .AddField(track.Title,
                        $"Played for {data.Player.State.CurrentPosition.Humanize(4, minUnit: TimeUnit.Second)}", true)
                    .WithThumbnail(track.GetThumbnail())
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder AddedTracksMessage(this MusiiGuild data, int tracks)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Songs added to the queue")
                    .WithColor(Success)
                    .WithDescription(tracks == 1? "1 song has been added to the queue!"
                        :$"{tracks} tracks have been added to the queue!")
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder SaveSessionMessage(this MusiiGuild data, bool unpinnedLeave = false)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle(unpinnedLeave?"All users have left the voice channel":"Your playlist has been saved into a record.")
                    .WithColor(Info)
                    .WithDescription($"Resume listening to this playlist by playing the record: \n`{data.Core.SaveRecord(data.Player.State.SaveRecord()).RecordId}` with the play command.")
                    .WithFooter(unpinnedLeave?"Pin the playlist to prevent this from happening":"Record Saved."));
        }
        public static DiscordMessageBuilder LockChangedMessage(this MusiiGuild data)
        {
            if (!data.Player.State.IsLocked)
            {
                return new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Playlist Unlocked")
                        .WithColor(DiscordColor.Green)
                        .WithDescription(
                            $"Any user will be able to interact with the playlist in this server")
                    );
            }
            else
            {
                return new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Playlist Locked")
                        .WithColor(DiscordColor.Red)
                        .WithDescription(
                            $"Only users with Manage Message permissions is able to interact with the playlist in this server.")
                    );
            }
        }
        public static DiscordMessageBuilder PinChangedMessage(this MusiiGuild data)
        {
            if (data.Player.State.IsPinned)
            {
                return new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Playlist Unpinned")
                        .WithColor(DiscordColor.Green)
                        .WithDescription(
                            $"The bot will leave when everyone leaves, or when the playlist is empty")
                    );
            }
            else
            {
                return new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Playlist Pinned")
                        .WithColor(DiscordColor.Red)
                        .WithDescription(
                            $"The bot will not leave until the playlist is empty")
                    );
            }
        }
        public static DiscordMessageBuilder QueueSkippedMessage(this MusiiGuild data, int skipped)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"Skipped {(skipped == 1?"Song":"Songs")}")
                    .WithColor(Success)
                    .WithDescription($"{skipped} {(skipped == 1?"song":"songs")} {(skipped == 1?"was":"were")} skipped.")
                    .WithFooter($"In channel {data.Player.Voice}"));
        }
        public static DiscordMessageBuilder PositionSetMessage(this MusiiGuild data, TimeSpan pos)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"Position Set")
                    .WithColor(Success)
                    .WithDescription($"The playhead position is set to {pos.Humanize(3)}")
                    .WithFooter($"In channel {data.Player.Voice}"));
        }
        public static DiscordMessageBuilder RecordRestoreMessage()
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Record restored")
                    .WithColor(Info)
                    .WithDescription($"A playlist record has been restored.")
                    .WithFooter($"Playback will continue."));
        }
        #endregion
        public static DiscordMessageBuilder GenericError(string title, string body, string footer)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(Error)
                    .WithDescription(body)
                    .WithFooter(footer));
        }
        public static DiscordMessageBuilder GenericSuccess(string title, string body, string footer)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(Success)
                    .WithDescription(body)
                    .WithFooter(footer));
        }
        public static DiscordMessageBuilder FailedToJoin(DiscordChannel voice)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Failed to join voice channel")
                    .WithColor(Error)
                    .WithDescription("Make sure the bot has the permissions to join the voice channel.")
                    .WithFooter($"In channel {voice}"));
        }
    }
}