using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using musii.Music;
using musii.Service;
using Newtonsoft.Json;

namespace musii.Modules
{
    public class MusicBotModule : ModuleBase<SocketCommandContext>
    {
        private MusicPlayerService _playerService;

        public MusicBotModule(MusicPlayerService svc)
        {
            _playerService = svc;
        }
        public static Dictionary<ulong, DateTime> Cooldown = new Dictionary<ulong, DateTime>();
        public static HashSet<ulong> LockedGuilds = new HashSet<ulong>();
        bool VerifyCooldowns(int seconds)
        {
            if (!Program.AuthorizedGuilds.ContainsKey(Context.Guild.Id))
            {
                ReplyAsync(embed: TextInterface.Unauthorized());
                return true;
            }
            var id = Context.User.Id;
            if(!Cooldown.ContainsKey(id)) Cooldown[id] = DateTime.MinValue;
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
            if (LockedGuilds.Contains(Context.Guild.Id) && !Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages && Context.User.Id != 236596516423204865)
            {
                ReplyAsync(embed: TextInterface.Locked());
                return true;
            }
            return false;
        }
        [Command("authorize", RunMode = RunMode.Async), RequireOwner]
        public Task Authorize()
        {
            Program.AuthorizedGuilds[Context.Guild.Id] = new GuildInfo(){Prefix = "!"};
            File.WriteAllTextAsync("authorized.json", JsonConvert.SerializeObject(Program.AuthorizedGuilds));
            return ReplyAsync(embed: TextInterface.Authorized());
        }
        [Command("unauthorize", RunMode = RunMode.Async), RequireOwner]
        public Task Unauthorize()
        {
            Program.AuthorizedGuilds.Remove(Context.Guild.Id);
            File.WriteAllTextAsync("authorized.json", JsonConvert.SerializeObject(Program.AuthorizedGuilds));
            return ReplyAsync("Guild has been unauthorized.");
        }
        [Command("prefix", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageMessages)]
        public Task Prefix(string s)
        {
            Program.AuthorizedGuilds[Context.Guild.Id].Prefix = s;
            File.WriteAllTextAsync("authorized.json", JsonConvert.SerializeObject(Program.AuthorizedGuilds));
            return ReplyAsync($"Guild Prefix has been changed to `{s}`");
        }
        [Command("play",  RunMode = RunMode.Async), Alias("p", "pl", "listen", "yt", "youtube","sp","spotify")] 
        public Task Play(params string[] keywords)
        {
            if(VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Play(Context, keywords);
            return _playerService.PlaySongAsync(Context, keywords);
        }
        
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Skip(Context, 1);
            return _playerService.SkipMusicAsync(1, Context);
        }
        [Command("skip",  RunMode = RunMode.Async), Alias("s")] 
        public Task Skip(int cnt)
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Skip(Context, cnt);
            return _playerService.SkipMusicAsync(cnt, Context);
        }
        [Command("leave",  RunMode = RunMode.Async), Alias("empty","clear","c","stop")] 
        public Task Clear()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            //return player.Clear(Context);
            return _playerService.ClearMusicAsync(Context);
        }
        [Command("queue",  RunMode = RunMode.Async), Alias("q","next")] 
        public Task Queue()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            //return player.ShowQueue(Context);
            return _playerService.GetQueueAsync(Context);
        }
        [Command("seek", RunMode = RunMode.Async), Alias("m", "move", "set")]
        public Task Seek(string time)
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            if (time.Length < 2) return ReplyAsync("Invalid Format, expected (-)#{s/m/h}. ex. `-4m`");
            var c = time[^1];
            if (int.TryParse(time.Substring(0, time.Length - 1), out var res) && (c == 's' || c == 'm' || c == 'h'))
            {
                bool neg = res < 0;
                res = Math.Abs(res);
                if (c == 's')
                {
                    return _playerService.SeekMusicAsync(Context, TimeSpan.FromSeconds(res), neg);
                }
                else if (c == 'm')
                {
                    return _playerService.SeekMusicAsync(Context, TimeSpan.FromMinutes(res), neg);
                }
                else
                {
                    return _playerService.SeekMusicAsync(Context, TimeSpan.FromHours(res), neg);
                }
            }
            else
            {
                return ReplyAsync("Invalid Format, expected (-)#{s/m/h}. ex. `-4m`");
            }
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            return ReplyAsync(embed: TextInterface.HelpMessage(Program.AuthorizedGuilds[Context.Guild.Id].Prefix));
        }
        [Command("help", RunMode = RunMode.Async)]
        public Task Help(params string[] k)
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            return ReplyAsync(embed: TextInterface.HelpMessage(Program.AuthorizedGuilds[Context.Guild.Id].Prefix));
        }

        [Command("loop", RunMode = RunMode.Async), Alias("l", "repeat", "lp")]
        public Task Loop()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return _playerService.LoopMusicAsync(Context);
        }
        [Command("pause", RunMode = RunMode.Async), Alias("hold", "suspend","ps")]
        public Task Pause()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return _playerService.PauseMusicAsync(Context);
        }

        [Command("v", RunMode = RunMode.Async), Alias("volume", "vol")]
        public async Task Volume(int amount)
        {
            if (VerifyCooldowns(1)) return;
            if (CheckPermissions()) return;
            if (amount > 1000 || amount < 0) await ReplyAsync("Invalid Volume");
            else
            {
                await _playerService.GetPlayer(Context.Guild).UpdateVolumeAsync((ushort)amount);
                await ReplyAsync("**Volume Set To:** `" + amount + "`%");
            }
        }
        [Command("v", RunMode = RunMode.Async), Alias("volume", "vol")]
        public Task Volume()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return ReplyAsync("**Volume:** `"+ _playerService.GetPlayer(Context.Guild).Volume+"`%");
        }

        [Command("shuffle", RunMode = RunMode.Async), Alias("r", "random", "mix")]
        public Task Shuffle()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (CheckPermissions()) return Task.CompletedTask;
            return _playerService.ShuffleMusicAsync(Context);
        }

        [Command("lock", RunMode = RunMode.Async)]
        public Task Lock()
        {
            if (VerifyCooldowns(1)) return Task.CompletedTask;
            if (Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages ||
                Context.User.Id == 236596516423204865)
            {
                if (!LockedGuilds.Contains(Context.Guild.Id))
                {
                    LockedGuilds.Add(Context.Guild.Id);
                    return ReplyAsync(embed: TextInterface.LockOn());
                }
                else
                {
                    LockedGuilds.Remove(Context.Guild.Id);
                    return ReplyAsync(embed: TextInterface.LockOff());
                }
            }
            else
            {
                return ReplyAsync(embed: TextInterface.LockedPermission());
            }
        }
    }
}
