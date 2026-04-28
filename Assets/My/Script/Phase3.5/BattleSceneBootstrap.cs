using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BattleEnvironmentSpawner environmentSpawner;
    [SerializeField] private BattlePlayerSetup playerSetup;
    [SerializeField] private BattleEnemySetup enemySetup;

    private IEnumerator Start()
    {
        yield return null;

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("No battle context found.");
            yield break;
        }

        BattleContextData context = BattleContextManager.Instance.CurrentContext;
        if (context == null || !context.IsValid())
        {
            Debug.LogWarning("Battle context is invalid.");
            yield break;
        }

        if (environmentSpawner != null)
            environmentSpawner.Spawn(context.environmentPrefab);

        int preparedPlayerCount = playerSetup != null ? playerSetup.PreparePlayerUnitsForDeployment(context) : 0;
        List<Unit> enemyUnits = enemySetup != null ? enemySetup.SpawnEnemyUnits(context) : new List<Unit>();

        Debug.Log("Battle bootstrap complete. PreparedPlayerUnits=" + preparedPlayerCount + " EnemyUnits=" + enemyUnits.Count);

        if (preparedPlayerCount == 0)
        {
            Debug.LogError("Battle start blocked: no player units were prepared for deployment.");
            yield break;
        }

        if (enemyUnits.Count == 0)
        {
            Debug.LogError("Battle start blocked: no enemy units were spawned.");
            yield break;
        }

        if (GameControl.EnableUnitDeployment())
        {
            UnitManager.GetInstance().deployingFacIdx = context.playerFactionSlotIndex;
            UnitManager.GetInstance().NewFactionDeployment();
            UIDeployment.ShowForCurrentDeployment();

            while (UnitManager.DeployingUnit())
                yield return null;
        }

        GameControl.ManualStartBattle();
    }
}