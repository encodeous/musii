using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Encodeous.Musii.Core;

namespace Encodeous.Musii.Commands
{
    [Description("Extra Controls"), RequireGuild]
    [RequireBotPermissions(Permissions.Speak | Permissions.EmbedLinks | Permissions.UseVoice | Permissions.SendMessages)]
    public class PlayerControlsModule : BaseCommandModule
    {
        private MusiiCore _sessions;

        public PlayerControlsModule(MusiiCore sessions)
        {
            _sessions = sessions;
        }
        
        [Command("pause")]
        [Description("Pause or Resume playback")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task PauseCommand(CommandContext ctx)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManMsgOrUnlocked, ctx)) return;

            if (await mgr.Player.TogglePause())
            {
                await ctx.RespondAsync(Messages.GenericSuccess("Toggled Playback", "Playback is now paused.", ""));
            }
            else
            {
                await ctx.RespondAsync(Messages.GenericSuccess("Toggled Playback", "Playback is now resumed.", ""));
            }
        }
        
        [Command("shuffle"), Aliases("r")]
        [Description("Shuffle the Playlist")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task ShuffleCommand(CommandContext ctx)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManMsgOrUnlocked, ctx)) return;

            await mgr.Player.ShuffleAsync();
            await ctx.RespondAsync(Messages.GenericSuccess("Shuffled", "The playlist has been shuffled", ""));
        }
        [Command("volume"), Aliases("v")]
        [Description("Set the volume, [0 - 100]% - For users with Manage Message Permissions, the max is 1,000%")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task VolumeCommand(CommandContext ctx, int volume)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManMsgOrUnlocked, ctx)) return;

            if (!ctx.HasPermission(Permissions.ManageMessages))
            {
                if (volume < 0 || volume > 100)
                {
                    await ctx.RespondAsync("Please specify a volume between [0, 100]");
                    return;
                }
            }
            else
            {
                if (volume < 0 || volume > 1000)
                {
                    await ctx.RespondAsync("Please specify a volume between [0, 1000]");
                    return;
                }
            }
            
            await mgr.Player.SetVolume(volume);
            await ctx.RespondAsync(Messages.GenericSuccess("Volume Set", $"Volume set to {volume}", ""));
        }
        [Command("save"), Aliases("sv", "rec", "record")]
        [Description("Saves the current playback into a record. Can be played across guilds.")]
        [Cooldown(3, 60, CooldownBucketType.Guild)]
        public async Task SaveCommand(CommandContext ctx)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel, ctx)) return;
            await ctx.RespondAsync(mgr.SaveSessionMessage());
        }
        [Command("seek"), Aliases("m", "move", "set")]
        [Description("Moves/sets the playhead position of the current song. Expected format: `[-]h:m:s[a]`. ex. `-0:23:0` or `1:10:0a` [a] stands for absolute time.")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task Seek(CommandContext ctx, string time)
        {
            var mgr = _sessions.GetSessionNew(ctx.Guild);
            if (await mgr.CheckIfFails(ExecutionFlags.RequireHasPlayer |
                                       ExecutionFlags.RequireVoicestate |
                                       ExecutionFlags.RequireSameVoiceChannel |
                                       ExecutionFlags.RequireManMsgOrUnlocked, ctx)) return;
            bool absolute = char.ToLower(time[^1]) == 'a';
            if (absolute) time = time[..^1];

            if (!TimeSpan.TryParse(time, out var ts))
            {
                await ctx.RespondAsync("Invalid format, expected format: `[-]h:m:s[a]`. ex. `-0:23:0` or `1:10:0a` [a] stands for absolute time.");
                return;
            }

            var nts = TimeSpan.Zero;
            if (!absolute) nts = mgr.Player.State.CurrentTrack.Position;
            nts += ts;
            if(nts <= TimeSpan.Zero) nts = TimeSpan.Zero;
            if (nts >= mgr.Player.State.CurrentTrack.Length) nts = mgr.Player.State.CurrentTrack.Length - TimeSpan.FromMilliseconds(500);

            await mgr.Player.SetPosition(nts);

            await ctx.RespondAsync(mgr.PositionSetMessage(nts));
        }
    }
}