using UnityEngine;

public class ColosseumSceneBootstrap : MonoBehaviour
{
    [Header("Auto Create Missing Managers")]
    [SerializeField] private bool createPlayerZoidTeamManagerIfMissing = true;
    [SerializeField] private bool createBattleContextManagerIfMissing = true;
    [SerializeField] private bool createColosseumManagerIfMissing = true;
    [SerializeField] private bool createColosseumEnemyGeneratorIfMissing = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        EnsureManagers();
    }

    private void Start()
    {
        EnsureManagers();

        ColosseumSetupUI setupUI = FindManager<ColosseumSetupUI>();
        if (setupUI != null)
        {
            setupUI.ForceRefreshReferencesFromScene();
        }
    }

    public void EnsureManagers()
    {
        UnitProgressManager progressManager = FindManager<UnitProgressManager>();
        if (progressManager == null && debugLog)
            Debug.LogWarning("[ColosseumSceneBootstrap] UnitProgressManager missing. Team ownership validation may fail.");

        PlayerZoidTeamManager teamManager = FindManager<PlayerZoidTeamManager>();
        if (teamManager == null && createPlayerZoidTeamManagerIfMissing)
        {
            GameObject obj = new GameObject("PlayerZoidTeamManager_AUTO");
            teamManager = obj.AddComponent<PlayerZoidTeamManager>();

            if (debugLog)
                Debug.Log("[ColosseumSceneBootstrap] Created PlayerZoidTeamManager_AUTO.");
        }

        BattleContextManager contextManager = FindManager<BattleContextManager>();
        if (contextManager == null && createBattleContextManagerIfMissing)
        {
            GameObject obj = new GameObject("BattleContextManager_AUTO");
            contextManager = obj.AddComponent<BattleContextManager>();

            if (debugLog)
                Debug.Log("[ColosseumSceneBootstrap] Created BattleContextManager_AUTO.");
        }

        ColosseumEnemyGenerator enemyGenerator = FindManager<ColosseumEnemyGenerator>();
        if (enemyGenerator == null && createColosseumEnemyGeneratorIfMissing)
        {
            GameObject obj = new GameObject("ColosseumEnemyGenerator_AUTO");
            enemyGenerator = obj.AddComponent<ColosseumEnemyGenerator>();

            if (debugLog)
                Debug.Log("[ColosseumSceneBootstrap] Created ColosseumEnemyGenerator_AUTO.");
        }

        ColosseumManager colosseumManager = FindManager<ColosseumManager>();
        if (colosseumManager == null && createColosseumManagerIfMissing)
        {
            GameObject obj = new GameObject("ColosseumManager_AUTO");
            colosseumManager = obj.AddComponent<ColosseumManager>();

            if (debugLog)
                Debug.Log("[ColosseumSceneBootstrap] Created ColosseumManager_AUTO.");
        }

        if (teamManager != null)
            teamManager.RefreshRuntimeReferences();

        if (colosseumManager != null)
            colosseumManager.RefreshRuntimeReferences();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }
}
