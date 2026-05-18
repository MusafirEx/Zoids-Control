using System;
using System.Collections.Generic;

[Serializable]
public class UnitProgressData
{
    public long createdAtUnix = 0;
    public long updatedAtUnix = 0;

    public List<UnitProgressEntry> unitData = new List<UnitProgressEntry>();

    public void Touch()
    {
        updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (createdAtUnix <= 0)
            createdAtUnix = updatedAtUnix;
    }

    public int GetDataAmount(int unitId)
    {
        for (int i = 0; i < unitData.Count; i++)
        {
            if (unitData[i].unitId == unitId)
                return unitData[i].dataAmount;
        }

        return 0;
    }

    public void SetDataAmount(int unitId, int amount)
    {
        amount = Math.Max(0, amount);

        for (int i = 0; i < unitData.Count; i++)
        {
            if (unitData[i].unitId == unitId)
            {
                unitData[i].dataAmount = amount;
                return;
            }
        }

        unitData.Add(new UnitProgressEntry(unitId, amount));
    }

    public void AddDataAmount(int unitId, int amount)
    {
        SetDataAmount(unitId, GetDataAmount(unitId) + amount);
    }
}
