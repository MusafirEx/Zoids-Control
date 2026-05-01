using System;

[Serializable]
public class UnitProgressEntry
{
    public int unitId;
    public int dataAmount;

    public UnitProgressEntry(int unitId, int dataAmount)
    {
        this.unitId = unitId;
        this.dataAmount = dataAmount;
    }
}
