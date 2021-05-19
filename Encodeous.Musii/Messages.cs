using System;
using DSharpPlus.Entities;
using Encodeous.Musii.Data;

namespace Encodeous.Musii
{
    public static class Messages
    {
        public static DiscordColor Success = DiscordColor.Green;
        public static DiscordColor Warning = DiscordColor.Purple;
        public static DiscordColor Error = DiscordColor.Red;
        #region Player Messages
        public static DiscordMessageBuilder PlaylistEmptyMessage(DiscordChannel channel)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Your playlist is empty.")
                    .WithColor(Warning)
                    .WithDescription("The bot will now leave the voice channel")
                    .WithFooter($"In {channel.Name}"));
        }
        public static DiscordMessageBuilder AddedTrackMessage(DiscordChannel channel, Track track)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Track added to the queue")
                    .WithColor(Success)
                    .AddField(track.Source.GetTitle(),
                        $"{track.Source.GetDuration():g}", true)
                    .WithThumbnail(track.Source.GetImageLink())
                    .WithFooter($"In {channel.Name}"));
        }
        public static DiscordMessageBuilder AddedTracksMessage(DiscordChannel channel, int tracks)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Tracks added to the queue")
                    .WithColor(Success)
                    .WithDescription(tracks == 1? "1 track has been added to the queue!"
                        :$"{tracks} tracks have been added to the queue!")
                    .WithFooter($"In {channel.Name}"));
        }
        #endregion
        public static DiscordMessageBuilder GenericError(DiscordChannel channel, string title, string body)
        {
            return new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(Error)
                    .WithDescription(body)
                    .WithFooter($"In {channel.Name}"));
        }
    }
}