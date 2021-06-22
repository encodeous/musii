namespace Encodeous.Musii.Core
{
    public enum TraceSource
    {
        None,
        LLWebsocketClose,
        LLPlaybackFinish,
        LLTrackException,
        LLTrackStuck,
        LLTrackUpdated,
        MPlayActive,
        MPlayPartialActive,
        MMoveNext,
        MShuffle,
        MPause,
        MLock,
        MLoop,
        MJump,
        MFilter,
        MSkip,
        MAdd,
        MStop,
        MVolume,
        MPin
    }
}