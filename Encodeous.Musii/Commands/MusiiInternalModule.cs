using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Encodeous.Musii.Data;
using Encodeous.Musii.Player;
using Microsoft.Extensions.Hosting;

namespace Encodeous.Musii.Commands
{
    [RequireOwner, Description("Musii internal commands"), Group("internal")]
    public class MusiiInternalModule : BaseCommandModule
    {
        [Group("bot")]
        public class MusiiInternalBotModule : BaseCommandModule
        {
            private IHostApplicationLifetime _lifetime;
            private DiscordClient _client;

            public MusiiInternalBotModule(IHostApplicationLifetime lifetime, DiscordClient client)
            {
                _lifetime = lifetime;
                _client = client;
            }

            [Command("shutdown"), Description("Shuts the bot down")]
            public async Task Shutdown(CommandContext ctx)
            {
                var msg = await ctx.RespondAsync(new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithTitle("Are you sure?")
                        .WithDescription($"Are you sure you want to shut down the bot?\n" +
                                         $"Please react with {DiscordEmoji.FromName(_client, ":white_check_mark:", false)} to shutdown, " +
                                         $"and {DiscordEmoji.FromName(_client, ":negative_squared_cross_mark:", false)} to cancel")
                        .WithFooter("Expires in 30 seconds")
                    ));
                await msg.CreateReactionAsync(DiscordEmoji.FromName(_client, ":white_check_mark:", false));
                await msg.CreateReactionAsync(DiscordEmoji.FromName(_client, ":negative_squared_cross_mark:", false));
                var res = await msg.WaitForReactionAsync(ctx.User, TimeSpan.FromSeconds(30));
                if (!res.TimedOut)
                {
                    if (res.Result.Emoji == DiscordEmoji.FromName(_client, ":white_check_mark:", false))
                    {
                        await ctx.RespondAsync("Shutting Down...");
                        _lifetime.StopApplication();
                    }
                    else
                    {
                        await ctx.RespondAsync("Cancelled");
                    }
                }
                else
                {
                    await ctx.RespondAsync("Timed Out");
                }
            }
        }

        [Group("player")]
        public class MusiiInternalPlayerModule : BaseCommandModule
        {
            private PlayerSessions _sessions;
            public MusiiInternalPlayerModule(PlayerSessions sessions)
            {
                _sessions = sessions;
            }
            [Command("play"), Description("Requests the bot to play a song, if it is not in another channel already")]
            public async Task BotPlayQuery(CommandContext ctx, [RemainingText] string query)
            {
                if (ctx.Member.VoiceState is null)
                {
                    await ctx.RespondAsync("You are not in a channel");
                    return;
                }
                var session = await _sessions.GetOrCreateSession(ctx.Guild, ctx.Member.VoiceState.Channel, ctx.Channel);
                if (!session.IsInitialized)
                {
                    await ctx.RespondAsync($"Session created!");
                }
                if (session.Data.VoiceChannel != ctx.Member.VoiceState.Channel)
                {
                    await ctx.RespondAsync("The bot is in another channel!");
                    return;
                }

                var result = await session.Searcher.ParseGeneralAsync(query);
                if (result.Item1 is null)
                {
                    await ctx.RespondAsync(Messages.GenericError("Query not found", result.Item2, ""));
                    return;
                }

                await session.AddTracks(result.Item1);
                await session.StartPlaying();
            }
        }
    }
}