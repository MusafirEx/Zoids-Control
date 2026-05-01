using System;

[Serializable]
public class BattleRewardUnitData
{
    public int unitId;
    public int dataAmount;

    public BattleRewardUnitData(int unitId, int dataAmount)
    {
        this.unitId = unitId;
        this.dataAmount = dataAmount;
    }
}
