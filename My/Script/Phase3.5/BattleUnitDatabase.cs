using System.Collections.Generic;
using UnityEngine;
using TBTK;

[CreateAssetMenu(fileName = "BattleUnitDatabase", menuName = "Zoids/Battle Unit Database")]
public class BattleUnitDatabase : ScriptableObject
{
    public List<BattleUnitDefinition> units = new List<BattleUnitDefinition>();

    public Unit GetUnitPrefab(int unitId)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].unitId == unitId)
                return units[i].unitPrefab;
        }

        return null;
    }
}
