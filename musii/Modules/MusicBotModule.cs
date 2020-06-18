using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using musii.Music;

namespace musii.Modules
{
    public class MusicBotModule : ModuleBase<SocketCommandContext>
    {
        public static MusicPlayer player = new MusicPlayer();

        [Command("play",  RunMode = RunMode.Async), Alias("p", "pl", "listen", "yt", "youtube")] 
        public Task Play(params string[] keywords)
        {
            return player.Play(Context, keywords);
        }
        
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip()
        {
            return player.Skip(Context, 1);
        }
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip(int cnt)
        {
            return player.Skip(Context, cnt);
        }
        [Command("leave",  RunMode = RunMode.Async), Alias("empty","clear","c")] 
        public Task Clear()
        {
            return player.Clear(Context);
        }
        [Command("queue",  RunMode = RunMode.Async), Alias("q")] 
        public Task Queue()
        {
            return player.ShowQueue(Context);
        }
        [Command("musii", RunMode = RunMode.Async)]
        public Task Invite()
        {
            return Context.User.SendMessageAsync(embed: GetEmbed());
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help()
        {
            if (Context.Guild.Id == 719734487415652382)
            {
                return Task.CompletedTask;
            }
            else
            {
                return ReplyAsync(embed:GetHelpEmbed());
            }
        }

        public static Embed GetEmbed()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = "Contribute to Musii | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "**Invite Musii**",
                Description =
                    "Thank you for showing interest in Musii.\n" +
                    "You can invite musii to your server with this link!\n" +
                    "https://discord.com/oauth2/authorize?client_id=709055409159405608&scope=bot&permissions=8",
                Footer = footer
            }.Build();
        }

        public static Embed GetHelpEmbed()
        {
            var footer = new EmbedFooterBuilder()
            {
                Text = "Contribute to Musii | https://github.com/encodeous/musii"
            };
            return new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "**Musii| Help**",
                Description =
                    $"**Commands**\n" +
                    $"  !help - Show help information\n" +
                    $"  !play [p, pl, listen, yt, youtube] <youtube-link> - Plays the youtube link in your current voice channel\n" +
                    $"  !s [skip] - Skips the active song\n" +
                    $"  !c [leave, empty, clear] - Clears the playback queue\n" +
                    $"  !q [queue] - Shows the songs in the queue\n" +
                    $"  !musii - Invite Musii to your server!\n",
                Footer = footer
            }.Build();
        }
    }
}
