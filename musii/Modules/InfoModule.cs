using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using musii.Utilities;

namespace musii.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
        {
            var st = (DateTime.Now - Program.StartTime);
            return ReplyAsync($"MUSII Bot {Config.VersionString}, Uptime: {MessageSender.TimeSpanFormat(st)}, Latency {Program._client.Latency}\n");
        }
    }
}
