# Work in Progress

## `What is Musii`

Musii is an alternative music bot to large public bots like Rythm or Groovy. It is a private, customizable bot that suits your needs.

## `Features`

In Musii v5, the features were overhauled and improved drastically

**Feature Highlights (WIP):**
- Convert Spotify Songs to YouTube for playback
- Archive playlists in "records" to be played later, or in another guild
- Advanced and intuitive playback controls
- Playlist "pinning" which supports 24/7 playback

**Offered Controls (WIP):**
- Movement of the playhead in real-time
- Adjust volume
- Apply bass / audio filters
- Looped Playback (Loop on current song, loop on playlist)
- Shuffle Playlist
- Skip / Skip Range
- Locking playlist to moderators
- Jump to a specific song
- 



## `Getting Started`

You can setup your own private music player with Musii in a few simple steps.

1. Setup [Lavalink](https://github.com/freyacodes/Lavalink) on your server. I also highly recommend reading this [blog post](https://blog.arbjerg.dev/2020/3/tunnelbroker-with-lavalink)!
2. Clone the repository and Compile Musii. For more information on how to build .NET apps, visit [.NET Docs](https://docs.microsoft.com/en-us/dotnet/fundamentals/)
3. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a bot
4. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/) and create an application
5. Run the bot, then edit `appsettings.json` with relevant information
As shown:

```json
{
...
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

