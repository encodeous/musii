using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using Encodeous.Musii.Core;

namespace Encodeous.Musii.Commands
{
    [Description("Advanced Diagnostic Module"), RequireGuild, RequireOwner]
    public class DiagnosticModule : BaseCommandModule
    {
        private MusiiCore _core;

        public DiagnosticModule(MusiiCore core)
        {
            _core = core;
        }

        [Priority(0)]
        [Command("trace")]
        [Description("Set the bot to trace in this channel")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task TraceCommand(CommandContext ctx, params string[] traceFilter)
        {
            var mgr = _core.GetMusiiGuild(ctx.Guild);
            if (await mgr.CheckIfFailsAsync(ExecutionFlags.RequireManageMessage, ctx)) return;
            if (!traceFilter.Any())
            {
                mgr.SetTraceDestination(null, new TraceSource[0]);
                await ctx.RespondAsync("Tracing disabled");
            }
            else
            {
                try
                {
                    var res = traceFilter.Select(x =>
                    {
                        var success = Enum.TryParse<TraceSource>(x, out var k);
                        if (success)
                        {
                            return k;
                        }
                        throw new Exception("");
                    });
                    mgr.SetTraceDestination(ctx.Channel, res.Distinct().ToArray());
                    await ctx.RespondAsync($"Tracing enabled for this channel with the filter: {string.Join(", ", res.Distinct().Select(x=>$"`{x}`"))}");
                }
                catch
                {
                    await ctx.RespondAsync(@$"Invalid filter, valid trace filters are: {
                        string.Join(", ",Enum.GetNames<TraceSource>().Select(x=>$"`{x}`"))}");
                }
            }
        }
        [Priority(1)]
        [Command("trace")]
        [Description("Set the bot to trace in this channel")]
        [Cooldown(2, 4, CooldownBucketType.Guild)]
        public async Task TraceCommand(CommandContext ctx)
        {
            var mgr = _core.GetMusiiGuild(ctx.Guild);
            if (await mgr.CheckIfFailsAsync(ExecutionFlags.RequireManageMessage, ctx)) return;
            await ctx.RespondAsync(@$"Tracing is disabled, valid trace filters are: {
                string.Join(", ",Enum.GetNames<TraceSource>().Select(x=>$"`{x}`"))}");
            mgr.SetTraceDestination(null, new TraceSource[0]);
        }
    }
}