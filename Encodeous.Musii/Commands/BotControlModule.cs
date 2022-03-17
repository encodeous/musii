using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Encodeous.Musii.Core;
using Microsoft.Extensions.Configuration;

namespace Encodeous.Musii.Commands;

[Description("Bot Controls")]
[RequireBotPermissions(Permissions.EmbedLinks | Permissions.SendMessages)]
public class BotControlModule : BaseCommandModule
{
    private MusiiCore _core;

    public BotControlModule(MusiiCore sessions)
    {
        _core = sessions;
    }
    
    [Command("authorize")]
    [Description("Authorizes a guild")]
    [RequireOwner]
    public async Task AuthorizeCommand(CommandContext ctx, ulong guildId)
    {
        _core.AuthorizeGuild(ctx, guildId);
        await ctx.RespondAsync("The guild has been authorized");
    }
    
    [Command("revoke")]
    [Description("Revoke the authorization of a guild")]
    [RequireOwner]
    public async Task RevokeCommand(CommandContext ctx, ulong guildId)
    {
        _core.RevokeGuild(ctx, guildId);
        await ctx.RespondAsync("The guild's authorization has been revoked.");
    }
}