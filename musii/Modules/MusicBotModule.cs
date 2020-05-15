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
    }
}
