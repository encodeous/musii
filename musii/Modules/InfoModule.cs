using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using musii.Music;
using musii.Utilities;

namespace musii.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
        {
            if (Context.Guild.Id == 719734487415652382)
            {
                return Task.CompletedTask;
            }
            else
            {
                return ReplyAsync(embed: GetEmbed());
            }
        }

        public static Embed GetEmbed()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = "Contribute to Musii | https://github.com/encodeous/musii"
            };
            var st = (DateTime.Now - Program.StartTime);
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "**Information**",
                Description =
                    $"Musii Bot {Config.VersionString}, Uptime:" +
                    $" {MessageSender.TimeSpanFormat(st)}, Latency {Program._client.Latency}\n" +
                    $"Type !help for help.",
                Footer = footer
            }.Build();
        }
    }
}
