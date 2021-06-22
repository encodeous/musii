﻿using System;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Data;
using Encodeous.Musii.Player;
using Humanizer;

namespace Encodeous.Musii
{
    public static class Messages
    {
        public static DiscordColor Success = DiscordColor.Green;
        public static DiscordColor Info = DiscordColor.Blurple;
        public static DiscordColor Warning = DiscordColor.Purple;
        public static DiscordColor Error = DiscordColor.Red;
        #region Player Messages
        public static DiscordMessageBuilder PlaylistEmptyMessage(this MusiiGuildManager data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playlist is empty.")
                    .WithColor(Warning)
                    .WithDescription("The bot will now leave the voice channel")
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder AddedTrackMessage(this MusiiGuildManager data, LavalinkTrack track)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Track added to the queue")
                    .WithColor(Success)
                    .AddField(track.Title,
                        $"{track.Length:g}", true)
                    .WithThumbnail(track.GetThumbnail())
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder AddedTracksMessage(this MusiiGuildManager data, int tracks)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Tracks added to the queue")
                    .WithColor(Success)
                    .WithDescription(tracks == 1? "1 track has been added to the queue!"
                        :$"{tracks} tracks have been added to the queue!")
                    .WithFooter($"In {data.Player.Voice.Name}"));
        }
        public static DiscordMessageBuilder SaveSessionMessage(this MusiiGuildManager data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playlist has been saved into a record.")
                    .WithColor(Info)
                    .WithDescription($"Resume listening to this playlist by playing the record: \n`{data.Sessions.SaveRecord(data.Player.State.SaveRecord()).RecordId}` with the play command.")
                    .WithFooter($"Record Saved."));
        }
        public static DiscordMessageBuilder LockChangedMessage(this MusiiGuildManager data)
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
        public static DiscordMessageBuilder QueueSkippedMessage(this MusiiGuildManager data, int skipped)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"Skipped {(skipped == 1?"Song":"Songs")}")
                    .WithColor(Success)
                    .WithDescription($"{skipped} {(skipped == 1?"song":"songs")} {(skipped == 1?"was":"were")} skipped.")
                    .WithFooter($"In channel {data.Player.Voice}"));
        }
        public static DiscordMessageBuilder PositionSetMessage(this MusiiGuildManager data, TimeSpan pos)
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