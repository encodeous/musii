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
            var session = await _sessions.GetOrCreateSession(ctx);
            if (!session.IsInitialized)
            {
                await ctx.RespondAsync($"Session created!");
                await session.InitializeStateAsync(_sessions.CreateState());
            }
            if (session.Channel != ctx.Member.VoiceState.Channel)
            {
                await ctx.RespondAsync("The bot is in another channel!");
                return;
            }

            var result = await session.Searcher.ParseGeneralAsync(query);
            if (result.Item1 is null)
            {
                await ctx.RespondAsync(Messages.GenericError(ctx.Channel, "Query not found", result.Item2));
                return;
            }

            if (result.Item1.Length == 1)
            {
                await session.AddTrack(result.Item1[0]);
            }
            else
            {
                await session.AddTracks(result.Item1);
            }

            await session.MoveNextAsync();
            session.StartPlaying();
        } 
    }
}