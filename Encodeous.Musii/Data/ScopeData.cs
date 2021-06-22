using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Nito.AsyncEx;

namespace Encodeous.Musii.Data
{
    public class ScopeData
    {
        internal LavalinkGuildConnection LavalinkNode;
        
        
        
        internal Func<Task> DeletePlayerCallback;
        internal DiscordChannel VoiceChannel, TextChannel;
        internal PlayerState State;
        internal AsyncManualResetEvent QueueUpdate = new (false);
    }
}