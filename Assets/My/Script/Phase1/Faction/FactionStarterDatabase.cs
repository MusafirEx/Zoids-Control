using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionStarterDatabase", menuName = "Zoids/Faction Starter Database")]
public class FactionStarterDatabase : ScriptableObject
{
    public List<FactionStarterData> factions = new List<FactionStarterData>();

    public FactionStarterData GetFaction(int factionId)
    {
        for (int i = 0; i < factions.Count; i++)
        {
            if (factions[i] != null && factions[i].factionId == factionId)
                return factions[i];
        }

        return null;
    }

    public bool HasFaction(int factionId)
    {
        return GetFaction(factionId) != null;
    }
}
