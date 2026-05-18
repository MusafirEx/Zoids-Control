using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ColosseumManager : MonoBehaviour
{
    public static ColosseumManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerZoidTeamManager teamManager;
    [SerializeField] private BattleContextManager battleContextManager;
    [SerializeField] private ColosseumEnemyGenerator enemyGenerator;
    [SerializeField] private ColosseumRewardManager rewardManager;

    [Header("Scene Names")]
    [SerializeField] private string colosseumSceneName = "ColosseumScene";
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string battleSceneName = "ZoidsBattleScene_JRPGStyle";

    [Header("Battle Context")]
    [SerializeField] private int colosseumContextAreaId = 999000;
    [SerializeField] private GameObject colosseumEnvironmentPrefab;
    [SerializeField] private int playerFactionId = 0;
    [SerializeField] private string playerFactionName = "Player";
    [SerializeField] private int enemyFactionId = 99;
    [SerializeField] private string enemyFactionName = "Colosseum Enemy";

    [Header("Runtime")]
    [SerializeField] private ColosseumRunData currentRun = new ColosseumRunData();

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public ColosseumRunData CurrentRun { get { return currentRun; } }

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RefreshRuntimeReferences();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (teamManager == null)
            teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();

        if (battleContextManager == null)
            battleContextManager = FindFirstObjectByTypeCompat<BattleContextManager>();

        if (enemyGenerator == null)
            enemyGenerator = GetComponent<ColosseumEnemyGenerator>();

        if (enemyGenerator == null)
            enemyGenerator = gameObject.AddComponent<ColosseumEnemyGenerator>();

        if (rewardManager == null)
            rewardManager = GetComponent<ColosseumRewardManager>();

        if (rewardManager == null)
            rewardManager = gameObject.AddComponent<ColosseumRewardManager>();

        if (debugLog)
        {
            if (teamManager == null)
                Debug.LogWarning("[ColosseumManager] PlayerZoidTeamManager not found.");

            if (battleContextManager == null)
                Debug.LogWarning("[ColosseumManager] BattleContextManager not found.");

            if (enemyGenerator == null)
                Debug.LogWarning("[ColosseumManager] ColosseumEnemyGenerator not found.");

            if (rewardManager == null)
                Debug.LogWarning("[ColosseumManager] ColosseumRewardManager not found.");
        }
    }

    public bool CanStartColosseum(int teamIndex, int battleSize, out string reason)
    {
        reason = "";

        RefreshRuntimeReferences();

        if (teamManager == null)
        {
            reason = "Team manager missing.";
            return false;
        }

        if (battleContextManager == null)
        {
            reason = "Battle context manager missing.";
            return false;
        }

        if (enemyGenerator == null)
        {
            reason = "Enemy generator missing.";
            return false;
        }

        if (battleSize < 1 || battleSize > 10)
        {
            reason = "Invalid battle size.";
            return false;
        }

        string teamReason;
        if (!teamManager.IsTeamValidForBattle(teamIndex, battleSize, out teamReason))
        {
            reason = teamReason;
            return false;
        }

        return true;
    }

    public bool StartColosseum(int teamIndex, int battleSize, int totalRounds)
    {
        string reason;
        if (!CanStartColosseum(teamIndex, battleSize, out reason))
        {
            Debug.LogWarning("[ColosseumManager] Cannot start colosseum. " + reason);
            return false;
        }

        totalRounds = Mathf.Max(1, totalRounds);

        currentRun.Reset();
        currentRun.active = true;
        currentRun.selectedTeamIndex = teamIndex;
        currentRun.battleSize = battleSize;
        currentRun.currentRound = 1;
        currentRun.totalRounds = totalRounds;
        currentRun.colosseumSceneName = colosseumSceneName;
        currentRun.loadingSceneName = loadingSceneName;
        currentRun.battleSceneName = battleSceneName;

        currentRun.fullTeamUnitIds = teamManager.GetTeamUnitIds(teamIndex);

        if (debugLog)
        {
            Debug.Log("[ColosseumManager] Starting colosseum. Team=" + (teamIndex + 1) +
                      " BattleSize=" + battleSize +
                      " Rounds=" + totalRounds +
                      " FullTeamCount=" + currentRun.fullTeamUnitIds.Count);
        }

        StartCurrentRound();
        return true;
    }

    public void StartCurrentRound()
    {
        if (!currentRun.active)
        {
            Debug.LogWarning("[ColosseumManager] Cannot start round. No active run.");
            return;
        }

        RefreshRuntimeReferences();

        currentRun.currentPlayerUnitIds = BuildPlayerRoundTeam(currentRun.fullTeamUnitIds, currentRun.battleSize);
        currentRun.currentEnemyUnitIds = enemyGenerator.GenerateEnemyTeam(
            currentRun.currentPlayerUnitIds,
            currentRun.battleSize,
            currentRun.currentRound,
            currentRun.totalRounds
        );

        if (currentRun.currentPlayerUnitIds.Count == 0 || currentRun.currentEnemyUnitIds.Count == 0)
        {
            Debug.LogError("[ColosseumManager] Cannot start round. Player or enemy team is empty.");
            return;
        }

        BattleContextData context = BuildBattleContext();

        battleContextManager.SetLoadingSceneName(loadingSceneName);
        battleContextManager.SetBattleSceneName(battleSceneName);
        battleContextManager.SetContext(context);
        battleContextManager.LoadLoadingScene();

        if (debugLog)
        {
            Debug.Log("[ColosseumManager] Round " + currentRun.currentRound + "/" + currentRun.totalRounds +
                      " started. PlayerUnits=" + currentRun.currentPlayerUnitIds.Count +
                      " EnemyUnits=" + currentRun.currentEnemyUnitIds.Count);
        }
    }

    private List<int> BuildPlayerRoundTeam(List<int> fullTeam, int battleSize)
    {
        List<int> result = new List<int>();

        if (fullTeam == null)
            return result;

        int count = Mathf.Min(battleSize, fullTeam.Count);

        // Player Zoids are NOT randomized.
        // Use the first N Zoids from selected team.
        for (int i = 0; i < count; i++)
            result.Add(fullTeam[i]);

        return result;
    }

    private BattleContextData BuildBattleContext()
    {
        BattleContextData context = new BattleContextData();

        context.areaId = colosseumContextAreaId;
        context.areaName = "Colosseum";
        context.battleType = "ColosseumBattle";
        context.environmentPrefab = colosseumEnvironmentPrefab;
        context.isNaturalArea = false;

        context.playerFactionId = playerFactionId;
        context.playerFactionName = playerFactionName;
        context.playerUnitIds = new List<int>(currentRun.currentPlayerUnitIds);

        context.enemyFactionId = enemyFactionId;
        context.enemyFactionName = enemyFactionName;
        context.enemyUnitIds = new List<int>(currentRun.currentEnemyUnitIds);

        context.playerFactionSlotIndex = 0;
        context.enemyFactionSlotIndex = 1;

        return context;
    }

    public void OnColosseumRoundFinished(bool playerWon)
    {
        if (!currentRun.active)
            return;

        if (!playerWon)
        {
            Debug.Log("[ColosseumManager] Colosseum failed at round " + currentRun.currentRound + ".");
            EndColosseum(false);
            return;
        }

        if (currentRun.IsFinalRound())
        {
            Debug.Log("[ColosseumManager] Colosseum completed.");
            EndColosseum(true);
            return;
        }

        currentRun.currentRound++;
        StartCurrentRound();
    }

    public void EndColosseum(bool playerWon)
    {
        if (debugLog)
            Debug.Log("[ColosseumManager] End colosseum. PlayerWon=" + playerWon);

        if (playerWon)
            ApplyColosseumClearBonus();

        currentRun.active = false;

        if (!string.IsNullOrEmpty(colosseumSceneName))
            SceneManager.LoadScene(colosseumSceneName);
    }


    private void ApplyColosseumClearBonus()
    {
        RefreshRuntimeReferences();

        if (rewardManager == null)
        {
            Debug.LogWarning("[ColosseumManager] ColosseumRewardManager missing. Clear bonus not applied.");
            return;
        }

        ColosseumBonusRewardResult result = rewardManager.ApplyClearBonus(currentRun);

        if (debugLog && result != null)
        {
            Debug.Log("[ColosseumManager] Clear bonus result. applied=" + result.applied +
                      " unitId=" + result.rewardUnitId +
                      " amount=" + result.dataAmount +
                      " message=" + result.message);
        }
    }

    public bool IsColosseumBattleContext(BattleContextData context)
    {
        return context != null && context.battleType == "ColosseumBattle";
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
