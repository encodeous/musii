using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace musii
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
        }

        public Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            return _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if (!(rawMessage.Channel is IGuildChannel)) return;
            int argPos = 0;

            string prefix = Program._config["prefix"];
            var gid = (rawMessage.Channel as IGuildChannel).GuildId;
            if (Program.AuthorizedGuilds.ContainsKey(gid)) prefix = Program.AuthorizedGuilds[gid].Prefix;
            if (!message.HasStringPrefix(prefix, ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider).ConfigureAwait(false);

            if (result.Error.HasValue && 
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString()).ConfigureAwait(false);
        }
    }
}
