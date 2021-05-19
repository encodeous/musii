using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Encodeous.Musii.Data;
using Encodeous.Musii.Player;

namespace Encodeous.Musii.Commands
{
    [RequireOwner, Description("Musii internal commands"), Group("internal")]
    public class MusiiInternalModule : BaseCommandModule
    {
        private PlayerSessions _sessions;

        public MusiiInternalModule(PlayerSessions sessions)
        {
            _sessions = sessions;
        }

        [Command("playq"), Description("Requests the bot to play a song, if it is not in another channel already")]
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
                await ctx.RespondAsync(session.Data.GenericError("Query not found", result.Item2));
                return;
            }

            await session.AddTracks(result.Item1);
            await session.StartPlaying();
        } 
    }
}