using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Encodeous.Musii.Core;

namespace Encodeous.Musii.Commands
{
    public static class ExecutionChecker
    {
        public static async Task<bool> CheckIfFailsAsync(this MusiiGuild manager, ExecutionFlags flags, CommandContext ctx)
        {
            if (!flags.HasFlag(ExecutionFlags.AuthorizationNotRequired))
            {
                if (!await manager.Core.CheckAuthorization(ctx))
                {
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireHasPlayer))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "There is no music playing!", $"", ""));
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireVoicestate))
            {
                if (ctx.Member.VoiceState is null)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "You are not in a channel", $"", ""));
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireSameVoiceChannel))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "There is no music playing!", $"", ""));
                    return true;
                }
                if (ctx.Member.VoiceState is null)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "You are not in a channel", $"", ""));
                    return true;
                }
                if (manager.Player.Voice != ctx.Member.VoiceState.Channel)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "The bot is in another channel!", $"", ""));
                    return true;
                }
            }

            if (flags.HasFlag(ExecutionFlags.RequireManageMessage))
            {
                if (!ctx.HasPermission(Permissions.ManageMessages) && !ctx.IsExecutedByBotOwner())
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "You do not have permission to execute this command!", $"", ""));
                    return true;
                }
            }
            if (flags.HasFlag(ExecutionFlags.RequireManMsgOrUnlocked))
            {
                if (!manager.HasPlayer)
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "There is no music playing!", $"", ""));
                    return true;
                }
                if (!ctx.HasPermission(Permissions.ManageMessages) && manager.Player.State.IsLocked && !ctx.IsExecutedByBotOwner())
                {
                    await ctx.RespondAsync(Messages.GenericError(
                        "The playlist is locked, you do not have access to it.", $"", ""));
                    return true;
                }
            }

            return false;
        }
    }
}