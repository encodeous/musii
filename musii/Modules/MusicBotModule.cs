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
        public static Dictionary<ulong, DateTime> Cooldown = new Dictionary<ulong, DateTime>();

        bool VerifyCooldowns(int seconds)
        {
            var id = Context.User.Id;
            if(!Cooldown.ContainsKey(id)) Cooldown[id] = DateTime.MinValue;
            //Context.User.Id != 236596516423204865 && 
            if (DateTime.Now - Cooldown[id] < TimeSpan.FromSeconds(seconds))
            {
                ReplyAsync(
                    $"**Please wait {(seconds - (DateTime.Now - Cooldown[id]).TotalSeconds):F} seconds before running this command again**");
                return true;
            }
            Cooldown[id] = DateTime.Now;
            return false;
        }

        bool CheckPermissions()
        {
            if (MusicManager.GetPlayer(Context).Locked && !Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages && Context.User.Id != 236596516423204865)
            {
                ReplyAsync(embed: TextInterface.Locked());
                return true;
            }
            return false;
        }

        [Command("play",  RunMode = RunMode.Async), Alias("p", "pl", "listen", "yt", "youtube","sp","spotify")] 
        public Task Play(params string[] keywords)
        {
            if(VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Play(Context, keywords);
            return MusicManager.GetPlayer(Context).PlayAsync(Context, keywords);
        }
        
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Skip(Context, 1);
            return MusicManager.GetPlayer(Context).SkipMusicAsync(1, Context);
        }
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip(int cnt)
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Skip(Context, cnt);
            return MusicManager.GetPlayer(Context).SkipMusicAsync(cnt, Context);
        }
        [Command("leave",  RunMode = RunMode.Async), Alias("empty","clear","c","stop")] 
        public Task Clear()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Clear(Context);
            return MusicManager.GetPlayer(Context).ClearMusicAsync(Context);
        }
        [Command("queue",  RunMode = RunMode.Async), Alias("q","next")] 
        public Task Queue()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            //return player.ShowQueue(Context);
            return MusicManager.GetPlayer(Context).GetQueueAsync(Context);
        }
        [Command("musii", RunMode = RunMode.Async)]
        public Task Invite()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            return Context.User.SendMessageAsync(embed: GetEmbed());
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            return ReplyAsync(embed: TextInterface.HelpMessage());
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help(params string[] k)
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            return ReplyAsync(embed: TextInterface.HelpMessage());
        }

        [Command("loop", RunMode = RunMode.Async), Alias("l", "repeat", "lp")]
        public Task Loop()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return MusicManager.GetPlayer(Context).LoopMusicAsync(Context);
        }

        [Command("shuffle", RunMode = RunMode.Async), Alias("r", "random", "mix")]
        public Task Shuffle()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return MusicManager.GetPlayer(Context).ShuffleMusicAsync(Context);
        }

        [Command("lock", RunMode = RunMode.Async)]
        public Task Lock()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages ||
                Context.User.Id == 236596516423204865)
            {
                MusicManager.GetPlayer(Context).Locked = !MusicManager.GetPlayer(Context).Locked;
                if (MusicManager.GetPlayer(Context).Locked)
                {
                    return ReplyAsync(embed: TextInterface.LockOn());
                }
                else
                {
                    return ReplyAsync(embed: TextInterface.LockOff());
                }
            }
            else
            {
                return ReplyAsync(embed: TextInterface.LockedPermission());
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
