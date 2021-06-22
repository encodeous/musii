# Work in Progress

## `What is Musii`

Musii is an alternative music bot to large public bots like Rythm. It is a private, customizable bot that suits your needs.

## `Features`

In Musii v5, the features were overhauled and improved drastically

**Feature Highlights:**
- Convert Spotify Songs to YouTube for playback
- Archive playlists in "records" to be played later
- Advanced and intuitive playback controls

**Offered Controls (WIP):**
- Movement of the playhead in real-time
- Adjust volume
- Apply bass / audio filters
- Looped Playback (Loop on current song, loop on playlist)
- Shuffle Playlist
- Skip / Skip Range
- Locking playlist to moderators



## `Getting Started`

You can setup your own private music player with Musii in a few simple steps.

1. Setup [Lavalink](https://github.com/Frederikam/Lavalink) on your server or use [F4stZ4p/HLavalink](https://github.com/F4stZ4p/HLavalink)
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

6. Execute Musii, make sure the LavaLink server is setup correctly and is reachable.

7. Invite the bot to your guild and `!help` to display help.



## `Libraries Used`

* [DSharpPlus/DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
* [Humanizr/Humanizer](https://github.com/Humanizr/Humanizer)
* [StephenCleary/AsyncEx](https://github.com/StephenCleary/AsyncEx)
* [JohnnyCrazy/SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET/)
* [Tyrrrz/YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
* [thomasgalliker/ResourceLoader](https://github.com/thomasgalliker/ResourceLoader)

