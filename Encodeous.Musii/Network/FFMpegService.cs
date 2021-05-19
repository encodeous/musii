using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.VoiceNext;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii.Network
{
    public class FFMpegService
    {
        public Func<TimeSpan, Task> ProgressUpdate;
        public CancellationToken FFMPegStop;
        public Action StopFFMpeg;
        private ILogger<FFMpegService> _log;

        public FFMpegService(ILogger<FFMpegService> log)
        {
            _log = log;
        }

        public async Task CreateFFMpeg(string streamUrl, IPEndPoint proxy, TimeSpan skip, byte[] buffer,
            VoiceTransmitSink dest, CancellationToken ct)
        {
            Action stop;
            var ffsource = new CancellationTokenSource();
            FFMPegStop = ffsource.Token;
            var pipe = new StreamPipeSink(async (stream, token) =>
            {
                var nct = CancellationTokenSource.CreateLinkedTokenSource(ct, token).Token;
                var br = new BufferedStream(stream, 100000);
                while (!nct.IsCancellationRequested)
                {
                    int len = await br.ReadAsync(buffer, nct);
                    if (nct.IsCancellationRequested || len == 0) break;
                    await dest.WriteAsync(buffer, 0, len, nct);
                }
            });

            try
            {
                var args = FFMpegArguments
                    .FromUrlInput(new Uri(streamUrl), o =>
                    {
                        o.Seek(skip);
                        o.WithCustomArgument($"-http_proxy http://{proxy.Address}:{proxy.Port}");
                        o.WithCustomArgument($" -user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36\"");
                        o.WithCustomArgument("-reconnect 1");
                        o.WithCustomArgument("-reconnect_streamed 1");
                        o.WithCustomArgument("-reconnect_delay_max 4");
                    })
                    .OutputToPipe(pipe, x =>
                    {
                        x.WithCustomArgument("-ac 2");
                        x.ForceFormat("s16le");
                        x.WithAudioSamplingRate();
                    }).NotifyOnProgress(async (x) =>
                    {
                        await ProgressUpdate.Invoke(x);
                    }).CancellableThrough(out stop);

                ct.Register(stop);
                await args.ProcessAsynchronously();
            }
            catch(Exception e)
            {
                _log.LogError($"FFMpeg has encountered an error: {e}");
            }

            ffsource.Cancel();
        }
    }
}