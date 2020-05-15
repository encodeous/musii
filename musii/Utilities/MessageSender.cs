using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace musii.Utilities
{
    class MessageSender
    {
        public static async Task SendLong(string s, ISocketMessageChannel channel)
        {
            if (s.Length >= 1990)
            {
                int len = -1;
                while (len != 0)
                {
                    len = Math.Min(s.Length, 1990);
                    await channel.SendMessageAsync(s.Substring(0, len)).ConfigureAwait(false);
                    await Task.Delay(500).ConfigureAwait(false);
                    s = s.Substring(len);
                }
            }
            else
            {
                await channel.SendMessageAsync(s).ConfigureAwait(false);
            }
        }

        public static string TimeSpanFormat(TimeSpan timeSpan)
        {
            return (Math.Floor(timeSpan.TotalHours).ToString("00")) + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00");
        }
    }
}
