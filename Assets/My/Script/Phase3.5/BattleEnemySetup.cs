using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class BattleEnemySetup : MonoBehaviour
{
    [SerializeField] private BattleUnitDatabase battleUnitDatabase;

    public List<Unit> SpawnEnemyUnits(BattleContextData context)
    {
        List<Unit> spawned = new List<Unit>();

        if (context == null || battleUnitDatabase == null)
        {
            Debug.LogWarning("BattleEnemySetup missing context or database.");
            return spawned;
        }

        int facSlot = context.enemyFactionSlotIndex;
        Debug.Log("Enemy factionSlot=" + facSlot + " unitCount=" + context.enemyUnitIds.Count);

        List<Faction> factions = UnitManager.GetFactionList();
        if (factions == null || facSlot < 0 || facSlot >= factions.Count)
        {
            Debug.LogWarning("Invalid enemy faction slot index: " + facSlot);
            return spawned;
        }

        Faction fac = UnitManager.GetFaction(facSlot);
        Debug.Log("Enemy faction found=" + (fac != null));

        if (fac == null)
        {
            Debug.LogWarning("Enemy faction not found in UnitManager at slot " + facSlot);
            return spawned;
        }

        List<Node> nodeList = GridManager.GetDeploymentNode(fac.factionID);
        Debug.Log("Enemy deployment node count=" + (nodeList != null ? nodeList.Count : 0) + " | deployFacID=" + fac.factionID);

        if (nodeList == null || nodeList.Count == 0)
        {
            Debug.LogWarning("No enemy deployment nodes found for deployFacID " + fac.factionID);
            return spawned;
        }

        for (int i = 0; i < context.enemyUnitIds.Count; i++)
        {
            if (nodeList.Count == 0) break;

            int unitId = context.enemyUnitIds[i];
            Unit prefab = battleUnitDatabase.GetUnitPrefab(unitId);
            if (prefab == null)
            {
                Debug.LogWarning("Missing enemy unit prefab for unitId=" + unitId);
                continue;
            }

            Node node = nodeList[0];
            nodeList.RemoveAt(0);

            GameObject clone = Instantiate(prefab.gameObject);
            Unit unit = UnitManager.PlaceUnit(clone, node, fac.direction, false);

            if (unit != null)
            {
                fac.unitList.Add(unit);
                spawned.Add(unit);
                Debug.Log("Spawned enemy unitId=" + unitId);
            }
        }

        return spawned;
    }
}