using System;
using UnityEngine;
using TBTK;

[Serializable]
public class BattleUnitDefinition
{
    public int unitId = 0;
    public string unitName = "New Unit";
    public Unit unitPrefab;

    public bool IsValid()
    {
        return unitPrefab != null;
    }
}
