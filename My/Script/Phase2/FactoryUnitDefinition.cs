using System;
using UnityEngine;

[Serializable]
public class FactoryUnitDefinition
{
    public int unitId = 0;
    public string unitName = "New Unit";
    public UnitRarity rarity = UnitRarity.Common;

    [Tooltip("How much data is required to manufacture one copy of this unit.")]
    public int manufactureCost = 100;

    [Tooltip("Optional note for design use only.")]
    [TextArea] public string notes = "";

    public bool IsValid()
    {
        return unitId >= 0 && manufactureCost > 0;
    }
}
