using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace musii.Music
{
    class MusicManager
    {
        private static Dictionary<ulong, PrivateMusicPlayer> _musicDictionary = new Dictionary<ulong, PrivateMusicPlayer>();

        public static PrivateMusicPlayer GetPlayer(SocketCommandContext context)
        {
            if (_musicDictionary.ContainsKey(context.Guild.Id))
            {
                return _musicDictionary[context.Guild.Id] = new PrivateMusicPlayer();
            }
            return _musicDictionary[context.Guild.Id];
        }
    }
}
