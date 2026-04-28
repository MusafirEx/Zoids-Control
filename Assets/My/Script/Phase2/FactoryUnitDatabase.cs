using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactoryUnitDatabase", menuName = "Zoids/Factory Unit Database")]
public class FactoryUnitDatabase : ScriptableObject
{
    public List<FactoryUnitDefinition> units = new List<FactoryUnitDefinition>();

    public FactoryUnitDefinition GetUnit(int unitId)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].unitId == unitId)
                return units[i];
        }

        return null;
    }

    public int GetManufactureCost(int unitId)
    {
        FactoryUnitDefinition def = GetUnit(unitId);
        return def != null ? def.manufactureCost : 100;
    }
}
