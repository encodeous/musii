## `What is Musii`

Musii is an alternative music bot to large public bots like Rythm. It is a private, customizable bot that suits your needs. 

## `Features`

Musii received a major feature update in v3, and in v4 there are more features than ever!

- Ability to play YouTube/Spotify songs and playlists.

- Ability to skip forward or backward in a track

- Custom Volume, from 0 to 1000%

- Shuffle Playback

- Looped Playback

- Pause/Resume Playback

- Locking Playback to Moderators

- Per-Guild command prefix

- Guild authentication by Bot owner

  

## `Getting Started`

You can setup your own private music player with Musii in a few simple steps.

1. Setup [Lavalink](https://github.com/Frederikam/Lavalink) on your server or use [F4stZ4p/HLavalink](https://github.com/F4stZ4p/HLavalink)
2. Clone and Compile Musii with `.NET Core 3.1`
3. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a bot
4. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/) and create an application
5. Create a file called `config.json` in the bot directory if it doesn't exist
6. Paste in relevant information

```json
{
  "token": "<token here>",
  "spotify_id": "<client id here>",
  "spotify_secret": "<secret here>",
  "prefix": "!",
  "lavalink_url": "localhost",
  "lavalink_port": "2333",
  "lavalink_auth": "youshallnotpass"
}
```

6. Execute Musii, make sure the lavalink server is setup correctly and is reachable.

7. Invite the bot to your guild and run `!authorize` to enable the bot.

   

## `Libraries Used`

* [discord-net/Discord.Net](https://github.com/discord-net/Discord.Net)
* [Yucked/Victoria](https://github.com/Yucked/Victoria)
* [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
* [JohnnyCrazy/SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET/)
* [Tyrrrz/YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)

