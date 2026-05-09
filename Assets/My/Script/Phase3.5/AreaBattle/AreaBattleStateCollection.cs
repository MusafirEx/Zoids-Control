using System;
using System.Collections.Generic;

[Serializable]
public class AreaBattleStateCollection
{
    public List<AreaBattleStateData> areas = new List<AreaBattleStateData>();

    public long globalPlayerAttemptLockedUntilUtcTicks = 0;
}
