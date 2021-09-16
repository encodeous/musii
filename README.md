# `Disclaimer`

In light of Google's recent crackdowns on Discord Music Bots,
Musii is likely in direct violation of YouTube's Terms Of Service.
Although Google most likely will not care about you running your own instance of Musii,
**you are still proceeding at your own risk**.

## `What is Musii`

Musii is an alternative music bot to large public bots like Rythm or Groovy. It is a private, customizable bot that suits your needs.

## `Features`

In Musii v5, the features were overhauled and improved drastically

**Main Features:**
- Automatic Spotify to YouTube conversion / playback
- Archive playlists in "records" to be played later, or in another guild
- Able to lock playback sessions, and only allow moderators to interact with them
- Playlist "pinning" which supports 24/7 playback
- Advanced event debugging/tracing

**Other Features:**
- Movement of the play-head while playing a song
- Adjust volume from `0%` to `1000%` (Moderator only above `100%`)
- Apply audio filters (`Bass`, `Piano`, `Metal`)
- Looped Playback (Loop on current song, loop on playlist)
- Shuffle Playlist
- Skip / Skip Range
- Jump forwards/backwards in the playlist
- Save playlist into a record
- Pause/Resume playback

## `Getting Started`

You can setup your own private music player with Musii in a few simple steps.

1. Setup [Lavalink](https://github.com/freyacodes/Lavalink) on your server. I also highly recommend reading this [blog post](https://blog.arbjerg.dev/2020/3/tunnelbroker-with-lavalink)!
2. Clone the repository and compile Musii. For more information on how to build .NET apps, visit [.NET Docs](https://docs.microsoft.com/en-us/dotnet/fundamentals/)
3. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a bot
4. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/) and create an application
5. Edit `appsettings.json` with relevant information. This file will be generated on the first run: (partially shown):

```json
{
  "musii": {
    "BotName": "Musii",
    "DefaultPrefix": "!",
    "DiscordToken": "",
    "SpotifyClientId": "",
    "SpotifyClientSecret": "",
    "LavalinkHost": "",
    "LavalinkPort": "",
    "LavalinkPassword": "",
    "DefaultQueueLength": "20",
    "InteractQueueTimeoutSeconds": "60"
  }
}
```

6. Execute Musii, make sure the Lavalink server is setup correctly and is reachable.

7. Invite the bot to your guild and `!help` to display help.



## `Libraries Used`

* [DSharpPlus/DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
* [Humanizr/Humanizer](https://github.com/Humanizr/Humanizer)
* [StephenCleary/AsyncEx](https://github.com/StephenCleary/AsyncEx)
* [JohnnyCrazy/SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET/)
* [Tyrrrz/YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
* [thomasgalliker/ResourceLoader](https://github.com/thomasgalliker/ResourceLoader)

