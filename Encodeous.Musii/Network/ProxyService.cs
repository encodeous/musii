using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ConcurrentCollections;
using Encodeous.DirtyProxy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Encodeous.Musii.Network
{
    public class ProxyService
    {
        private ILogger<ProxyService> _logger;
        private List<IPEndPoint> _proxies = new ();
        private ConcurrentQueue<IPEndPoint> _queue;
        private ConcurrentHashSet<IPAddress> _addresses = new();
        public ProxyService(ILogger<ProxyService> logger, IHostApplicationLifetime lifetime)
        {
            _queue = new ConcurrentQueue<IPEndPoint>();
            _logger = logger;
            if (!File.Exists("cachedproxies.txt"))
            {
                _logger.LogWarning("No proxies found! Musii will load some proxies... This might take a few minutes");
                ProxyScraper.CheckTasks = 700;
                var scraper = new ProxyScraper(ProxyScraper.DefaultList, async prox =>
                {
                    var httpClient = new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy(prox.ToString())
                    });
                    try
                    {
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.UserAgent);
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                        var yt = new YoutubeClient(httpClient);
                        await yt.Videos.Streams.GetManifestAsync(
                            VideoId.Parse("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), cts.Token);
                        if (!cts.IsCancellationRequested)
                        {
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                    finally
                    {
                        httpClient.Dispose();
                    }
                    return false;
                });
                lifetime.ApplicationStopping.Register(() =>
                {
                    _logger.LogWarning("Proxy scraping interrupted! This process will resume on the next start");
                    scraper.Stop();
                });
                var result = scraper.ScrapeAsync().GetAwaiter().GetResult();
                File.WriteAllText("cachedproxies.txt", 
                    string.Join("\n",result.ValidProxies.Select(x=>$"{x.Address}:{x.Port}")));
                _proxies = result.ValidProxies.ToList();
            }
            else
            {
                string s = File.ReadAllText("cachedproxies.txt");
                foreach (var s1 in s.Split('\n'))
                {
                    _proxies.Add(new IPEndPoint(IPAddress.Parse(s1.Split(':')[0]), int.Parse(s1.Split(':')[1])));
                }
            }
            _logger.LogInformation("Sorting proxies by ping... If users play music before this is finished, they may experience slower response times");

            var channel = Channel.CreateUnbounded<IPEndPoint>();
            foreach(var k in _proxies)
            {
                channel.Writer.TryWrite(k);
            }
            Task.Run(async () =>
            {
                while (channel.Reader.Count != 0)
                {
                    var thing = await channel.Reader.ReadAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                    await TimeProxy(thing);
                }
            });
        }
        private async Task TimeProxy(IPEndPoint prox)
        {
            try
            {
                if(_addresses.Contains(prox.Address)) return;
                _addresses.Add(prox.Address);
                var httpClient = new HttpClient(new HttpClientHandler()
                {
                    Proxy = new WebProxy(prox.Address.ToString(), prox.Port)
                });
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.UserAgent);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var yt = new YoutubeClient(httpClient);
                await yt.Videos.Streams.GetManifestAsync(
                    VideoId.Parse("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), cts.Token);
                if (!cts.IsCancellationRequested)
                {
                    _queue.Enqueue(prox);
                }
            }
            catch(Exception e)
            {

            }
        }

        public async Task<ProxyWrapper> GetProxy(CancellationToken ct)
        {
            IPEndPoint proxy;
            while (!_queue.TryDequeue(out proxy) && !ct.IsCancellationRequested) await Task.Delay(100);
            if (ct.IsCancellationRequested) return null;
            var wrapper = new ProxyWrapper((x) =>
            {
                _queue.Enqueue(x);
            }, proxy);
            return wrapper;
        }
    }
}