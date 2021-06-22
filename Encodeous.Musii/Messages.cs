using System;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Data;

namespace Encodeous.Musii
{
    public static class Messages
    {
        public static DiscordColor Success = DiscordColor.Green;
        public static DiscordColor Info = DiscordColor.Blurple;
        public static DiscordColor Warning = DiscordColor.Purple;
        public static DiscordColor Error = DiscordColor.Red;
        #region Player Messages
        public static DiscordMessageBuilder PlaylistEmptyMessage(this ScopeData data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playlist is empty.")
                    .WithColor(Warning)
                    .WithDescription("The bot will now leave the voice channel")
                    .WithFooter($"In {data.VoiceChannel.Name}"));
        }
        public static DiscordMessageBuilder AddedTrackMessage(this ScopeData data, LavalinkTrack track)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Track added to the queue")
                    .WithColor(Success)
                    .AddField(track.Title,
                        $"{track.Length:g}", true)
                    .WithThumbnail(track.GetThumbnail())
                    .WithFooter($"In {data.VoiceChannel.Name}"));
        }
        public static DiscordMessageBuilder AddedTracksMessage(this ScopeData data, int tracks)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Tracks added to the queue")
                    .WithColor(Success)
                    .WithDescription(tracks == 1? "1 track has been added to the queue!"
                        :$"{tracks} tracks have been added to the queue!")
                    .WithFooter($"In {data.VoiceChannel.Name}"));
        }
        public static DiscordMessageBuilder SaveSessionMessage(this ScopeData data)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playback session has been saved.")
                    .WithColor(Info)
                    .WithDescription($"Resume listening to this playlist by running the play command with \n `{data.State.StateId}`")
                    .WithFooter($"Session saved."));
        }
        public static DiscordMessageBuilder SessionResumeMessage()
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Playlist Resumed")
                    .WithColor(Info)
                    .WithDescription($"The playlist will resume playing.")
                    .WithFooter($"Success."));
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
    }
}