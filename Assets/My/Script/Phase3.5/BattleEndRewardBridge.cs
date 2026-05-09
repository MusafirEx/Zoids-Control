using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TBTK;

public class BattleEndRewardBridge : MonoBehaviour
{
    [Header("Faction Slots In Battle Scene")]
    [SerializeField] private int playerFactionSlotIndex = 0;
    [SerializeField] private int enemyFactionSlotIndex = 1;

    [Header("Reward")]
    [SerializeField] private BattleRewardManager rewardManager;

    [Header("Optional Return Scene")]
    [SerializeField] private bool returnToSceneAfterResult = false;
    [SerializeField] private string returnSceneName = "DummyMapSelector";
    [SerializeField] private float returnDelay = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool battleHasStarted = false;
    private bool resultApplied = false;
    private float resultTime = -1f;

    private void Reset()
    {
        rewardManager = FindObjectOfType<BattleRewardManager>();
    }

    private void Awake()
    {
        if (rewardManager == null)
            rewardManager = FindObjectOfType<BattleRewardManager>();
    }

    private void Update()
    {
        if (resultApplied)
        {
            HandleReturnScene();
            return;
        }

        if (!IsReadyToWatchBattleEnd())
            return;

        int playerAlive = GetAliveUnitCount(playerFactionSlotIndex);
        int enemyAlive = GetAliveUnitCount(enemyFactionSlotIndex);

        if (debugLog && !battleHasStarted)
            Debug.Log("[BattleEndRewardBridge] Battle watch started. PlayerAlive=" + playerAlive + " EnemyAlive=" + enemyAlive);

        battleHasStarted = true;

        if (playerAlive <= 0)
        {
            ApplyBattleResult(false);
            return;
        }

        if (enemyAlive <= 0)
        {
            ApplyBattleResult(true);
            return;
        }
    }

    private bool IsReadyToWatchBattleEnd()
    {
        if (UnitManager.GetInstance() == null)
            return false;

        if (UnitManager.DeployingUnit())
            return false;

        if (!battleHasStarted)
        {
            // Before TBTK.StartGame(), selected unit is normally null.
            // This prevents false loss during manual deployment.
            if (UnitManager.GetSelectedUnit() == null)
                return false;

            if (GetAliveUnitCount(playerFactionSlotIndex) <= 0)
                return false;

            if (GetAliveUnitCount(enemyFactionSlotIndex) <= 0)
                return false;
        }

        return true;
    }

    private int GetAliveUnitCount(int factionSlotIndex)
    {
        List<Faction> factions = UnitManager.GetFactionList();
        if (factions == null || factionSlotIndex < 0 || factionSlotIndex >= factions.Count)
            return 0;

        Faction faction = factions[factionSlotIndex];
        if (faction == null || faction.unitList == null)
            return 0;

        int count = 0;
        for (int i = 0; i < faction.unitList.Count; i++)
        {
            Unit unit = faction.unitList[i];
            if (unit == null) continue;
            if (unit.hp <= 0) continue;
            if (unit.node == null) continue;

            count++;
        }

        return count;
    }

    private void ApplyBattleResult(bool playerWon)
    {
        if (resultApplied)
            return;

        resultApplied = true;
        resultTime = Time.time;

        List<int> rewardEnemyUnitIds = GetRewardEnemyUnitIds();

        if (debugLog)
        {
            string ids = rewardEnemyUnitIds != null ? string.Join(",", rewardEnemyUnitIds) : "null";
            Debug.Log("[BattleEndRewardBridge] Battle ended. PlayerWon=" + playerWon + " RewardEnemyUnitIds=" + ids);
        }

        if (rewardManager == null)
        {
            Debug.LogWarning("[BattleEndRewardBridge] BattleRewardManager is missing. Reward not applied.");
            return;
        }

        BattleResultData result = rewardManager.BuildResult(playerWon, rewardEnemyUnitIds);
        rewardManager.ApplyResult(result);

        if (debugLog && result != null)
        {
            string summary = playerWon ? "WIN" : "LOSS";
            summary += " rewards:";

            for (int i = 0; i < result.rewards.Count; i++)
            {
                BattleRewardUnitData reward = result.rewards[i];
                summary += " [unit " + reward.unitId + " +" + reward.dataAmount + "]";
            }

            Debug.Log("[BattleEndRewardBridge] " + summary + " | total=" + result.GetTotalRewardData());
        }
    }

    private List<int> GetRewardEnemyUnitIds()
    {
        List<int> ids = new List<int>();

        if (BattleContextManager.Instance != null &&
            BattleContextManager.Instance.HasContext &&
            BattleContextManager.Instance.CurrentContext != null &&
            BattleContextManager.Instance.CurrentContext.enemyUnitIds != null)
        {
            ids.AddRange(BattleContextManager.Instance.CurrentContext.enemyUnitIds);
        }

        return ids;
    }

    private void HandleReturnScene()
    {
        if (!returnToSceneAfterResult)
            return;

        if (string.IsNullOrEmpty(returnSceneName))
            return;

        if (resultTime < 0)
            return;

        if (Time.time < resultTime + returnDelay)
            return;

        SceneManager.LoadScene(returnSceneName);
    }
}
