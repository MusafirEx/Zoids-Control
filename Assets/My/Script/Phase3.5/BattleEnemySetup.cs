using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class BattleEnemySetup : MonoBehaviour
{
    [SerializeField] private BattleUnitDatabase battleUnitDatabase;

    [Header("Spawn Placement")]
    [SerializeField] private bool randomizeEnemyDeploymentNodes = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public List<Unit> SpawnEnemyUnits(BattleContextData context)
    {
        List<Unit> spawned = new List<Unit>();

        if (context == null)
        {
            Debug.LogWarning("[BattleEnemySetup] Missing context.");
            return spawned;
        }

        if (context.enemyUnitIds == null || context.enemyUnitIds.Count == 0)
        {
            Debug.LogWarning("[BattleEnemySetup] Context has no enemy unit IDs.");
            return spawned;
        }

        int facSlot = context.enemyFactionSlotIndex;

        Debug.Log("[BattleEnemySetup] Enemy setup start. battleType=" + context.battleType +
                  " enemyFactionSlot=" + facSlot +
                  " contextEnemyFactionId=" + context.enemyFactionId +
                  " enemyUnitCount=" + context.enemyUnitIds.Count +
                  " enemyIds=" + FormatIdList(context.enemyUnitIds));

        List<Faction> factions = UnitManager.GetFactionList();
        if (factions == null || facSlot < 0 || facSlot >= factions.Count)
        {
            Debug.LogWarning("[BattleEnemySetup] Invalid enemy faction slot index: " + facSlot +
                             " factionListCount=" + (factions != null ? factions.Count : 0));
            return spawned;
        }

        Faction fac = UnitManager.GetFaction(facSlot);
        if (fac == null)
        {
            Debug.LogWarning("[BattleEnemySetup] Enemy faction not found at slot " + facSlot);
            return spawned;
        }

        fac.unitList.Clear();
        fac.deployingList.Clear();

        List<Node> nodeList = GetBestEnemyDeploymentNodes(context, fac, facSlot);

        if (nodeList == null || nodeList.Count == 0)
        {
            Debug.LogWarning("[BattleEnemySetup] No enemy deployment nodes found. " +
                             "Check battle scene grid deployment/faction IDs. " +
                             "Tried fac.factionID=" + fac.factionID +
                             ", facSlot=" + facSlot +
                             ", context.enemyFactionId=" + context.enemyFactionId);
            return spawned;
        }

        // Important:
        // GridManager.GetDeploymentNode may return the original deployment node list.
        // Make a copy first, so random shuffle does not permanently change the original grid list.
        nodeList = new List<Node>(nodeList);

        if (randomizeEnemyDeploymentNodes)
        {
            ShuffleNodes(nodeList);
        }

        Debug.Log("[BattleEnemySetup] Enemy deployment node count=" + nodeList.Count +
                  " selectedFacID=" + fac.factionID +
                  " facSlot=" + facSlot +
                  " randomize=" + randomizeEnemyDeploymentNodes);

        for (int i = 0; i < context.enemyUnitIds.Count; i++)
        {
            if (nodeList.Count == 0)
            {
                Debug.LogWarning("[BattleEnemySetup] Not enough enemy deployment nodes for all enemy units.");
                break;
            }

            int unitId = context.enemyUnitIds[i];

            Unit prefab = GetUnitPrefabForBattle(unitId, context.battleType);
            if (prefab == null)
            {
                Debug.LogWarning("[BattleEnemySetup] Missing enemy unit prefab for unitId=" + unitId +
                                 ". Add this ID to BattleUnitDatabase or make sure UnitDB prefabID exists.");
                continue;
            }

            Node node = nodeList[0];
            nodeList.RemoveAt(0);

            Unit unit = UnitManager.PlaceUnit(prefab.gameObject, node, fac.direction, true);
            if (unit == null)
            {
                Debug.LogWarning("[BattleEnemySetup] Failed to spawn enemy unitId=" + unitId +
                                 " prefab=" + prefab.gameObject.name);
                continue;
            }

            unit.SetFacID(fac.factionID);
            unit.playableUnit = fac.playableFaction;

            if (!fac.unitList.Contains(unit))
                fac.unitList.Add(unit);

            spawned.Add(unit);

            Debug.Log("[BattleEnemySetup] Spawned enemy unitId=" + unitId +
                      " prefab=" + prefab.gameObject.name +
                      " node=" + node);
        }

        Debug.Log("[BattleEnemySetup] Enemy setup complete. Spawned=" + spawned.Count);
        return spawned;
    }

    private List<Node> GetBestEnemyDeploymentNodes(BattleContextData context, Faction fac, int facSlot)
    {
        List<Node> nodes;

        nodes = GridManager.GetDeploymentNode(fac.factionID);
        if (nodes != null && nodes.Count > 0)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Using deployment nodes from fac.factionID=" + fac.factionID);
            return nodes;
        }

        nodes = GridManager.GetDeploymentNode(facSlot);
        if (nodes != null && nodes.Count > 0)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Using deployment nodes from facSlot=" + facSlot);
            return nodes;
        }

        nodes = GridManager.GetDeploymentNode(context.enemyFactionId);
        if (nodes != null && nodes.Count > 0)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Using deployment nodes from context.enemyFactionId=" + context.enemyFactionId);
            return nodes;
        }

        // Common enemy deployment IDs in test scenes.
        nodes = GridManager.GetDeploymentNode(1);
        if (nodes != null && nodes.Count > 0)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Using fallback deployment nodes from ID=1");
            return nodes;
        }

        nodes = GridManager.GetDeploymentNode(2);
        if (nodes != null && nodes.Count > 0)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Using fallback deployment nodes from ID=2");
            return nodes;
        }

        return new List<Node>();
    }

    private void ShuffleNodes(List<Node> nodes)
    {
        if (nodes == null || nodes.Count <= 1)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            int randomIndex = Random.Range(i, nodes.Count);

            Node temp = nodes[i];
            nodes[i] = nodes[randomIndex];
            nodes[randomIndex] = temp;
        }
    }

    private Unit GetUnitPrefabForBattle(int unitId, string battleType)
    {
        if (battleUnitDatabase != null)
        {
            Unit prefab = battleUnitDatabase.GetUnitPrefab(unitId);
            if (prefab != null)
            {
                if (debugLog)
                    Debug.Log("[BattleEnemySetup] Found unitId=" + unitId + " in BattleUnitDatabase.");
                return prefab;
            }
        }

        Unit unitDbPrefab = UnitDB.GetPrefab(unitId);
        if (unitDbPrefab != null)
        {
            if (debugLog)
                Debug.Log("[BattleEnemySetup] Found unitId=" + unitId + " in UnitDB fallback.");
            return unitDbPrefab;
        }

        return null;
    }

    private string FormatIdList(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return "empty";

        string result = "";
        for (int i = 0; i < ids.Count; i++)
        {
            result += ids[i].ToString();
            if (i < ids.Count - 1)
                result += ",";
        }

        return result;
    }
}