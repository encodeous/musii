using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Encodeous.Musii.Core;

namespace Encodeous.Musii.Commands;

[Description("Bot Controls")]
[RequireBotPermissions(Permissions.EmbedLinks | Permissions.SendMessages)]
[RequireOwner]
public class BotControlModule : BaseCommandModule
{
    private MusiiCore _core;
    private DiscordClient _client;

    public BotControlModule(MusiiCore sessions, DiscordClient client)
    {
        _core = sessions;
        _client = client;
    }
    
    [Command("authorize")]
    [Description("Authorizes a guild")]
    public async Task AuthorizeCommand(CommandContext ctx, ulong guildId)
    {
        _core.AuthorizeGuild(ctx, guildId);
        await ctx.RespondAsync("The guild has been authorized");
    }
    
    [Command("invite")]
    [Description("Gets the invite link")]
    public async Task InviteCommand(CommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle("Musii Invite Link")
            .WithDescription($"https://discord.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=139657079872&scope=bot"));
    }
    
    [Command("revoke")]
    [Description("Revoke the authorization of a guild")]
    public async Task RevokeCommand(CommandContext ctx, ulong guildId)
    {
        _core.RevokeGuild(ctx, guildId);
        await ctx.RespondAsync("The guild's authorization has been revoked.");
    }

    [Description("Server Controls")]
    [Group("server")]
    [RequireOwner]
    public class ServerModule : BaseCommandModule
    {
        private MusiiCore _core;
        private DiscordClient _client;

        public ServerModule(MusiiCore sessions, DiscordClient client)
        {
            _core = sessions;
            _client = client;
        }
        [Command("list")]
        [Description("Lists all of the servers that the bot is in.")]
        public async Task ListCommand(CommandContext ctx)
        {
            var sb = new StringBuilder();
            foreach (var g in _client.Guilds)
            {
                if (string.IsNullOrEmpty(g.Value.IconUrl))
                {
                    sb.AppendLine(
                        $"`{g.Key}` - Name: `{g.Value.Name}`, Owner: {g.Value.Owner.Mention}, User Count: `{g.Value.MemberCount}`");
                }
                else
                {
                    sb.AppendLine(
                        $"`{g.Key}` - Name: `{g.Value.Name}`, Owner: {g.Value.Owner.Mention}, User Count: `{g.Value.MemberCount}`, Icon: `{g.Value.IconUrl}`");
                }

            }

            await ctx.RespondAsync(new DiscordMessageBuilder()
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Servers: ")
                    .WithDescription(sb.ToString())));
        }
        
        [Command("users")]
        [Description("Lists all of the users in a guild")]
        public async Task ListUsersCommand(CommandContext ctx, ulong guildId)
        {
            var members = _client.Guilds[guildId].Members;
            var pages = new List<Page>();
            int i = 1;
            foreach (var m in members.Chunk(20))
            {
                pages.Add(new Page(embed: new DiscordEmbedBuilder()
                    .WithTitle($"Users of `{guildId}` - Page {i} / {Math.Ceiling(members.Count / 20.0)}")
                    .WithDescription(string.Join("\n", m.Select(val =>
                    {
                        return $"`{val.Key}` - {val.Value.Mention}";
                    })))));
                i++;
            }

            await ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages, new PaginationEmojis());
        }
        
        [Command("leave")]
        [Description("Leaves a guild")]
        public async Task LeaveGuildCommand(CommandContext ctx, ulong guildId)
        {
            var guild = _client.Guilds[guildId];

            var msg = await ctx.RespondAsync($"Are you sure you want to leave `{guild.Id}` - `{guild.Name}`? React within 5 seconds.");
            var emoji = DiscordEmoji.FromName(_client, ":white_check_mark:");
            await msg.CreateReactionAsync(emoji);
            var res = await msg.WaitForReactionAsync(ctx.User, emoji, TimeSpan.FromSeconds(5));

            if (res.TimedOut)
            {
                await ctx.RespondAsync("Timed out.");
            }
            else
            {
                await guild.LeaveAsync();
                await ctx.RespondAsync("The bot has left the guild.");
            }
        }
    }
}