using DSharpPlus.Lavalink;

namespace Encodeous.Musii
{
    public class Constants
    {
        public const string BotName = "Musii";

        // Reference: https://github.com/PythonistaGuild/Wavelink/blob/master/wavelink/eqs.py
        public static readonly LavalinkBandAdjustment[] PUNCH_BASS = {
            new (0, -0.075f), new(1, .125f), new(2, .125f), new(3, .1f), new(4, .1f),
            new(5, .05f), new(6, 0.075f), new(7, 0), new(8, 0), new(9, 0),
            new(10, 0), new(11, 0), new(12, .125f), new(13, .15f), new(14, .05f)
        };
        public static readonly LavalinkBandAdjustment[] METAL_ROCK = {
            new (0, .0f), new(1, .1f), new(2, .1f), new(3, .15f), new(4, .13f),
            new (5, .1f), new(6, .0f), new(7, .125f), new(8, .175f), new(9, .175f),
            new (10, .125f), new(11, .125f), new(12, .1f), new(13, .075f), new(14, .0f)
        };
        public static readonly LavalinkBandAdjustment[] PIANO = {
            new (0, -0.25f), new(1, -0.25f), new(2, -0.125f), new(3, 0.0f),
            new (4, 0.25f), new(5, 0.25f), new(6, 0.0f), new(7, -0.25f), new(8, -0.25f),
            new (9, 0.0f), new(10, 0.0f), new(11, 0.5f), new(12, 0.25f), new(13, -0.025f)
        };
    }
}