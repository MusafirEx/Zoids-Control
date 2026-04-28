using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IntValueEntry
{
    public int id;
    public int value;

    public IntValueEntry(int id, int value)
    {
        this.id = id;
        this.value = value;
    }
}

[Serializable]
public class PlayerProfileData
{
    public string playerId = "";
    public string playerName = "";
    public bool profileInitialized = false;

    public int chosenFactionId = -1;
    public string chosenFactionName = "";

    public long createdAtUnix = 0;
    public long updatedAtUnix = 0;

    public long nextAreaBattleUnix = 0;

    public List<IntValueEntry> ownedUnits = new List<IntValueEntry>();
    public List<int> activeTeamUnitIds = new List<int>();

    public List<int> clearedStageIds = new List<int>();
    public List<int> unlockedAreaIds = new List<int>();

    public void Touch()
    {
        updatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (createdAtUnix <= 0)
            createdAtUnix = updatedAtUnix;
    }

    public int GetOwnedCount(int unitId)
    {
        for (int i = 0; i < ownedUnits.Count; i++)
        {
            if (ownedUnits[i].id == unitId)
                return ownedUnits[i].value;
        }

        return 0;
    }

    public void SetOwnedCount(int unitId, int count)
    {
        count = Mathf.Max(0, count);

        for (int i = 0; i < ownedUnits.Count; i++)
        {
            if (ownedUnits[i].id == unitId)
            {
                ownedUnits[i].value = count;
                return;
            }
        }

        ownedUnits.Add(new IntValueEntry(unitId, count));
    }

    public void AddOwnedCount(int unitId, int amount)
    {
        SetOwnedCount(unitId, GetOwnedCount(unitId) + amount);
    }

    public bool HasUnitInActiveTeam(int unitId)
    {
        return activeTeamUnitIds.Contains(unitId);
    }

    public bool HasClearedStage(int stageId)
    {
        return clearedStageIds.Contains(stageId);
    }

    public void MarkStageCleared(int stageId)
    {
        if (!clearedStageIds.Contains(stageId))
            clearedStageIds.Add(stageId);
    }

    public bool HasUnlockedArea(int areaId)
    {
        return unlockedAreaIds.Contains(areaId);
    }

    public void UnlockArea(int areaId)
    {
        if (!unlockedAreaIds.Contains(areaId))
            unlockedAreaIds.Add(areaId);
    }
}
