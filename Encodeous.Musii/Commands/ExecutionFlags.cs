using System;

namespace Encodeous.Musii.Commands
{
    [Flags]
    public enum ExecutionFlags
    {
        RequireVoicestate = 0,
        RequireSameVoiceChannel = 1 << 0,
        RequireHasPlayer = 1 << 1,
        RequireManageMessage = 1 << 2,
        RequireManMsgOrUnlocked = 1 << 3
    }
}