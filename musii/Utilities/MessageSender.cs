using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace musii.Utilities
{
    class MessageSender
    {
        public static string TimeSpanFormat(TimeSpan timeSpan)
        {
            return (Math.Floor(timeSpan.TotalHours).ToString("00")) + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00");
        }
    }
}
