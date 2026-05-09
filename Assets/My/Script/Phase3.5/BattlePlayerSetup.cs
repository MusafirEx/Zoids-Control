using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class BattlePlayerSetup : MonoBehaviour
{
    [SerializeField] private BattleUnitDatabase battleUnitDatabase;

    public int PreparePlayerUnitsForDeployment(BattleContextData context)
    {
        if (context == null || battleUnitDatabase == null)
        {
            Debug.LogWarning("BattlePlayerSetup missing context or database.");
            return 0;
        }

        int facSlot = context.playerFactionSlotIndex;
        Debug.Log("Player factionSlot=" + facSlot + " unitCount=" + context.playerUnitIds.Count);

        List<Faction> factions = UnitManager.GetFactionList();
        if (factions == null || facSlot < 0 || facSlot >= factions.Count)
        {
            Debug.LogWarning("Invalid player faction slot index: " + facSlot);
            return 0;
        }

        Faction fac = UnitManager.GetFaction(facSlot);
        if (fac == null)
        {
            Debug.LogWarning("Player faction not found at slot " + facSlot);
            return 0;
        }

        fac.unitList.Clear();
        fac.deployingList.Clear();

        int preparedCount = 0;

        for (int i = 0; i < context.playerUnitIds.Count; i++)
        {
            int unitId = context.playerUnitIds[i];

            Unit prefab = battleUnitDatabase.GetUnitPrefab(unitId);
            if (prefab == null)
            {
                Debug.LogWarning("Missing player unit prefab for unitId=" + unitId);
                continue;
            }

            GameObject clone = Instantiate(prefab.gameObject, new Vector3(0, 99999, 0), Quaternion.identity);
            clone.name = prefab.gameObject.name + "_PlayerDeploy";
            clone.transform.parent = UnitManager.GetInstance().transform;

            Unit unit = clone.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogWarning("Player prefab has no Unit component. unitId=" + unitId);
                Destroy(clone);
                continue;
            }

            unit.SetFacID(fac.factionID);
            unit.playableUnit = fac.playableFaction;

            fac.deployingList.Add(unit);
            preparedCount++;

            Debug.Log("Prepared player deploy unitId=" + unitId);
        }

        return preparedCount;
    }
}
