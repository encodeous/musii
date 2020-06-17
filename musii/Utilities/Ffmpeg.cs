using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace musii.Utilities
{
    class Ffmpeg
    {
        public static Process CreateFfmpeg(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ProcessStartInfo proc = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 2 -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                };
                return Process.Start(proc);
            }
            else
            {
                ProcessStartInfo proc = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-hide_banner -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 2 -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                };
                return Process.Start(proc);
            }
        }
    }
}
