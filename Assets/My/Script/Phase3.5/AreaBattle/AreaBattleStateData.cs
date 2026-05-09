using System;
using System.Collections.Generic;

[Serializable]
public class AreaBattleStateData
{
    public int areaId = -1;

    public int ownerFactionId = -1;
    public string ownerFactionName = "";

    public List<int> defenderUnitIds = new List<int>();

    public long areaLockedUntilUtcTicks = 0;
    public long playerAttemptLockedUntilUtcTicks = 0;

    public string lastBattleUtc = "";
    public bool lastPlayerWon = false;

    public bool IsAreaLocked()
    {
        return DateTime.UtcNow.Ticks < areaLockedUntilUtcTicks;
    }

    public bool IsPlayerAttemptLocked()
    {
        return DateTime.UtcNow.Ticks < playerAttemptLockedUntilUtcTicks;
    }

    public TimeSpan GetAreaLockRemaining()
    {
        long remaining = areaLockedUntilUtcTicks - DateTime.UtcNow.Ticks;
        return remaining > 0 ? new TimeSpan(remaining) : TimeSpan.Zero;
    }

    public TimeSpan GetPlayerAttemptRemaining()
    {
        long remaining = playerAttemptLockedUntilUtcTicks - DateTime.UtcNow.Ticks;
        return remaining > 0 ? new TimeSpan(remaining) : TimeSpan.Zero;
    }
}
