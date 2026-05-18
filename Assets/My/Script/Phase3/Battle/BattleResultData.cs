using System;
using System.Collections.Generic;

[Serializable]
public class BattleResultData
{
    public bool playerWon = false;
    public List<int> defeatedEnemyUnitIds = new List<int>();
    public List<BattleRewardUnitData> rewards = new List<BattleRewardUnitData>();

    public int GetTotalRewardData()
    {
        int total = 0;
        for (int i = 0; i < rewards.Count; i++)
            total += rewards[i].dataAmount;
        return total;
    }
}
