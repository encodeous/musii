using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Encodeous.Musii.Data;
using Encodeous.Musii.Network;
using Microsoft.Extensions.Logging;

namespace Encodeous.Musii.Player
{
    /// <summary>
    /// A player represents a playing session. Players will execute a PlayerState
    /// </summary>
    public class MusicPlayer : IDisposable
    {
        public SearchService Searcher;
        public bool IsInitialized { get; private set; }
        public bool IsPlaying { get; private set; }
        // Set by PlayerSessions
        internal ProxyWrapper Proxy;
        internal Action DeletePlayer;
        internal DiscordChannel Channel, TextChannel;
        
        private bool _isDisposed = false;
        private VoiceNextConnection _connection;
        private ILogger<MusicPlayer> _log;
        private PlayerState _state;
        private CancellationTokenSource _playTaskSwitch = new CancellationTokenSource();
        private CancellationToken _playTask;
        private FFMpegService _ffmpeg;
        private HttpClient _client;
        private VoiceTransmitSink _tsmSink;

        public MusicPlayer(ILogger<MusicPlayer> log, FFMpegService ffmpeg, SearchService searcher)
        {
            _log = log;
            _ffmpeg = ffmpeg;
            Searcher = searcher;
            _playTask = _playTaskSwitch.Token;
        }

        public async Task Connect(DiscordChannel channel, DiscordChannel textChannel)
        {
            _client = new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy(Proxy.EndPoint.Address.ToString())
            });
            Channel = channel;
            TextChannel = textChannel;
            _connection = await channel.ConnectAsync();
            await _connection.SendSpeakingAsync();
            _connection.VoiceSocketErrored += (sender, args) =>
            {
                _log.LogDebug($"Client in channel {Channel.Name} Has exited with exception of: {args.Exception.Message}");
                return Task.CompletedTask;
            };
            _connection.UserLeft += (sender, args) =>
            {
                _log.LogDebug($"User {args.User.Username} in channel {Channel.Name} has left");
                return Task.CompletedTask;
            };
            Task.Run(async () =>
            {
                while (!_isDisposed)
                {
                    await Task.Delay(500);
                    if ((bool) _connection.GetType().GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_connection))
                    {
                        _log.LogDebug($"Client in channel {Channel.Name} has been disposed");
                        Dispose();
                        break;
                    }
                }
            });
        }
        
        /// <summary>
        /// Attach a state to the player (Allows for pause/resume, channel moving, auto-reconnections, session saving... etc)
        /// </summary>
        /// <param name="state"></param>
        public async Task InitializeStateAsync(PlayerState state)
        {
            IsInitialized = true;
            _state = state;
            _playTaskSwitch?.Cancel();
        }
        
        /// <summary>
        /// Starts playing music, must already have a song!
        /// </summary>
        public void StartPlaying()
        {
            if (IsPlaying) return;
            if (_state?.CurrentTrack is null) throw new Exception("Music player started playing without a state!");
            IsPlaying = true;
            Task.Run(AudioPlayer);
        }
        
        /// <summary>
        /// All tracks have been played
        /// </summary>
        private async Task PlaylistEndedAsync()
        {
            await TextChannel.SendMessageAsync(Messages.PlaylistEmptyMessage(TextChannel));
        }

        public async Task AddTrack(Track track)
        {
            await TextChannel.SendMessageAsync(Messages.AddedTrackMessage(TextChannel, track));
            await _state.StateLock.WaitAsync();
            try
            {
                _state.Tracks.Add(track);
            }
            finally
            {
                _state.StateLock.Release();
            }
        }
        
        public async Task AddTracks(Track[] tracks)
        {
            await TextChannel.SendMessageAsync(Messages.AddedTracksMessage(TextChannel, tracks.Length));
            await _state.StateLock.WaitAsync();
            try
            {
                _state.Tracks.AddRange(tracks);
            }
            finally
            {
                _state.StateLock.Release();
            }
        }

        public void Skip()
        {
            _playTaskSwitch.Cancel();
            _playTaskSwitch = new CancellationTokenSource();
            _playTask = _playTaskSwitch.Token;
        }

        public async Task MoveNextAsync()
        {
            await _state.StateLock.WaitAsync();
            try
            {
                if (!_state.Tracks.Any())
                {
                    await PlaylistEndedAsync();
                    Dispose();
                }
                var cur = _state.CurrentTrack;
                _state.CurrentTrack = _state.Tracks.First();
                _state.Tracks.Remove(_state.CurrentTrack);
                if (_state.IsLooped && cur is not null)
                {
                    cur.Position = TimeSpan.Zero;
                    _state.Tracks.Add(cur);
                }
            }
            finally
            {
                _state.StateLock.Release();
            }
        }
        
        
        /// <summary>
        /// Worker task that plays music :)
        /// </summary>
        private async Task AudioPlayer()
        {
            try
            {
                _ffmpeg.ProgressUpdate += span =>
                {
                    _state.CurrentTrack.Position = span;
                    return Task.CompletedTask;
                };
                _tsmSink = _connection.GetTransmitSink();
                var buf = new byte[64000];
                while (!_isDisposed)
                {
                    await _ffmpeg.CreateFFMpeg(await _state.CurrentTrack.Source.GetStreamUrl(_client), Proxy.EndPoint,
                        _state.CurrentTrack.Position, buf, _tsmSink, _playTask);
                    await MoveNextAsync();
                }
            }
            catch(Exception e)
            {
                _log.LogError($"Exception occurred while playing audio: {e}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _playTaskSwitch.Cancel();
                _connection.Disconnect();
                _tsmSink?.Dispose();
                Proxy.Dispose();
                DeletePlayer.Invoke();
            }
        }
    }
}