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
        //public static MusicPlayer player = new MusicPlayer();

        [Command("play",  RunMode = RunMode.Async), Alias("p", "pl", "listen", "yt", "youtube","sp","spotify")] 
        public Task Play(params string[] keywords)
        {
            //return player.Play(Context, keywords);
            return MusicManager.GetPlayer(Context).PlayAsync(Context, keywords);
        }
        
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip()
        {
            //return player.Skip(Context, 1);
            return MusicManager.GetPlayer(Context).SkipMusicAsync(1, Context);
        }
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip(int cnt)
        {
            //return player.Skip(Context, cnt);
            return MusicManager.GetPlayer(Context).SkipMusicAsync(cnt, Context);
        }
        [Command("leave",  RunMode = RunMode.Async), Alias("empty","clear","c","stop")] 
        public Task Clear()
        {
            //return player.Clear(Context);
            return MusicManager.GetPlayer(Context).ClearMusicAsync(Context);
        }
        [Command("queue",  RunMode = RunMode.Async), Alias("q","next")] 
        public Task Queue()
        {
            //return player.ShowQueue(Context);
            return MusicManager.GetPlayer(Context).GetQueueAsync(Context);
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
                return ReplyAsync(embed: TextInterface.HelpMessage());
            }
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help(params string[] k)
        {
            if (Context.Guild.Id == 719734487415652382)
            {
                return Task.CompletedTask;
            }
            else
            {
                return ReplyAsync(embed: TextInterface.HelpMessage());
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
    }
}
