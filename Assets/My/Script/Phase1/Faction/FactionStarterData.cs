using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionStarterData
{
    [Header("Faction")]
    public int factionId = 0;
    public string factionName = "New Faction";
    public Sprite factionLogo;
    public Sprite FactionMainLogo;
    public Color factionColor;

    [Header("Starter Team")]
    [Tooltip("Unit IDs given immediately when this faction is chosen.")]
    public List<int> starterUnitIds = new List<int>();

    [Tooltip("How many owned copies to grant for each starter unit.")]
    public int starterOwnedCountPerUnit = 1;

    [Tooltip("Optional first team name shown/used for this faction.")]
    public string starterTeamName = "Starter Team";

    public bool IsValid()
    {
        return factionId >= 0 && starterUnitIds != null && starterUnitIds.Count > 0;
    }
}
