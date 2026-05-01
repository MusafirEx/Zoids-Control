using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BattleEnvironmentSpawner environmentSpawner;
    [SerializeField] private BattlePlayerSetup playerSetup;
    [SerializeField] private BattleEnemySetup enemySetup;

    void Start()
    {
        //yield return null;

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("No battle context found.");
            //yield break;
        }

        BattleContextData context = BattleContextManager.Instance.CurrentContext;
        if (context == null || !context.IsValid())
        {
            Debug.LogWarning("Battle context is invalid.");
            //yield break;
        }

        if (environmentSpawner != null)
            environmentSpawner.Spawn(context.environmentPrefab);

        int preparedPlayerCount = playerSetup != null ? playerSetup.PreparePlayerUnitsForDeployment(context) : 0;
        int preparedEnemyCount = enemySetup != null ? enemySetup.PrepareEnemyUnitsForDeployment(context) : 0;

        Debug.Log("Battle bootstrap complete. PreparedPlayerUnits=" + preparedPlayerCount + " EnemyUnits=" + preparedPlayerCount);

        if (preparedPlayerCount == 0)
        {
            Debug.LogError("Battle start blocked: no player units were prepared for deployment.");
            //yield break;
        }

        if (preparedEnemyCount == 0)
        {
            Debug.LogError("Battle start blocked: no player units were prepared for deployment.");
            //yield break;
        }


       
      //GameControl.instance.Begining();
    }
}