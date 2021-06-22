using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Encodeous.Musii.Player;

namespace Encodeous.Musii.Commands
{
    public static class ExecutionChecker
    {
        public static async Task<bool> CheckIfFails(this MusiiGuildManager manager, ExecutionFlags flags, CommandContext ctx)
        {
            if (flags.HasFlag(ExecutionFlags.RequireHasPlayer))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync("There is no music playing!");
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireVoicestate))
            {
                if (ctx.Member.VoiceState is null)
                {
                    await ctx.RespondAsync("You are not in a channel");
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireSameVoiceChannel))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync("There is no music playing!");
                    return true;
                }
                if (ctx.Member.VoiceState is null)
                {
                    await ctx.RespondAsync("You are not in a channel");
                    return true;
                }
                if (manager.Player.Voice != ctx.Member.VoiceState.Channel)
                {
                    await ctx.RespondAsync("The bot is in another channel!");
                    return true;
                }
            }

            if (flags.HasFlag(ExecutionFlags.RequireManageMessage))
            {
                if (!ctx.HasPermission(Permissions.ManageMessages))
                {
                    await ctx.RespondAsync("You do not have permission to execute this command!");
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireManMsgOrUnlocked))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync("There is no music playing!");
                    return true;
                }
                if (!ctx.HasPermission(Permissions.ManageMessages) && manager.Player.State.IsLocked)
                {
                    await ctx.RespondAsync("The playlist is locked, you do not have access to it.");
                    return true;
                }
            }

            return false;
        }
    }
}