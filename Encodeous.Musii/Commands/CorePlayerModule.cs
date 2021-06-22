﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Encodeous.Musii.Core;

namespace Encodeous.Musii.Commands
{
    [Description("Player Controls"), RequireGuild]
    [RequireBotPermissions(Permissions.Speak | Permissions.EmbedLinks | Permissions.UseVoice | Permissions.SendMessages)]
    public class CorePlayerModule : BaseCommandModule
    {
        private MusiiCore _sessions;

        public CorePlayerModule(MusiiCore sessions)
        {
            _sessions = sessions;
        }
        
        [Command("play")]
        [Aliases("p","pl"), Description("Request to play a song")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task PlayCommand(CommandContext ctx, [RemainingText] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await ctx.RespondAsync("Please specify a valid query");
                return;
            }
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireVoicestate, ctx)) return;
            if (mgr.HasPlayer && mgr.Player.Voice != ctx.Member.VoiceState.Channel)
            {
                await ctx.RespondAsync("The bot is in another channel!");
                return;
            }

            bool freshSession = false;
            if (Guid.TryParse(query.Trim(), out var x))
            {
                if (mgr.HasPlayer)
                {
                    await ctx.RespondAsync("Before restoring a record, please clear your current playlist.");
                    return;
                }
                var state = _sessions.RestoreRecord(x);
                if (state is null)
                {
                    await ctx.RespondAsync("Cannot restore playlist, it does not exist!");
                    return;
                }

                freshSession = true;
                await mgr.CreatePlayerAsync(ctx.Member.VoiceState.Channel,ctx.Channel, state);
                await ctx.RespondAsync(Messages.RecordRestoreMessage());
            }
            else
            {
                if (!mgr.HasPlayer)
                {
                    freshSession = true;
                    await mgr.CreatePlayerAsync(ctx.Member.VoiceState.Channel, ctx.Channel);
                }
                var result = await mgr.GetSearcher().ParseGeneralAsync(query);
                if (result.Item1 is null)
                {
                    await ctx.RespondAsync(Messages.GenericError("Query not found", result.Item2, "Error"));
                    // dumb restriction by lavalink that the bot must join a channel first for it to make queries
                    if (freshSession)
                    {
                        await mgr.StopAsync();
                    }
                    return;
                }
                await mgr.Player.AddTracks(result.Item1, ctx);
            }

            if (freshSession)
            {
                // start playing :)
                if(mgr.Player.State.CurrentTrack is null) await mgr.Player.MoveNextAsync();
                await mgr.Player.PlayActiveSongAsync();
            }
        }
        [Command("queue"), Aliases("q"), Description("Shows the playlist, expires after 1 minute of inactivity"), Cooldown(2, 4, CooldownBucketType.Guild)]
        [RequireBotPermissions(Permissions.ManageMessages)]
        public async Task QueueCommand(CommandContext ctx, int page = 1)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel, ctx)) return;
        
            await mgr.Player.SendQueueMessage(page);
        }
        [Command("lock"), Aliases("dj")]
        [Description("Toggles lock on playback commands to users with Manage Message permissions.")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task LockCommand(CommandContext ctx)
        {
            if (ctx.Member.VoiceState is null)
            {
                await ctx.RespondAsync("You are not in a channel");
                return;
            }
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManageMessage, ctx)) return;
            await ctx.RespondAsync(mgr.LockChangedMessage());
            await mgr.Player.ToggleLock();
        }
        [Priority(1)]
        [Command("skip")]
        [Aliases("s")]
        [Description("Skips song(s) (including currently played song), or within a range")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public Task SkipCommand(CommandContext ctx, int count = 1)
        {
            return SkipCommand(ctx, 0, count - 1);
        }
        [Priority(0)]
        [Command("skip")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task SkipCommand(CommandContext ctx, int lowerBound, int upperBound)
        {
            if (ctx.Member.VoiceState is null)
            {
                await ctx.RespondAsync("You are not in a channel");
                return;
            }
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManMsgOrUnlocked, ctx)) return;
            if (lowerBound > upperBound || lowerBound < 0 || upperBound > mgr.Player.State.Tracks.Count + 1)
            {
                await ctx.RespondAsync("Please specify a valid range");
                return;
            }

            if (await mgr.Player.SkipSongs(lowerBound, upperBound))
            {
                await ctx.RespondAsync(mgr.QueueSkippedMessage(upperBound - lowerBound + 1));
            }
        }
    }
}