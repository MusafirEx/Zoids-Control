using System.Collections.Generic;
using UnityEngine;

public class BattleRewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;
    [SerializeField] private UnitRewardCalculator rewardCalculator;
    [SerializeField] private ZoidsPerkRewardBridge perkRewardBridge;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Header("Auto Reference")]
    [SerializeField] private bool autoCreateUnitProgressManagerIfMissing = true;

    [Header("Game Jolt Cloud Save")]
    [SerializeField] private bool uploadCloudSaveAfterReward = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (progressManager == null)
            progressManager = FindManager<UnitProgressManager>();

        if (progressManager == null && autoCreateUnitProgressManagerIfMissing)
        {
            GameObject obj = new GameObject("UnitProgressManager_AUTO");
            progressManager = obj.AddComponent<UnitProgressManager>();

            if (debugLog)
                Debug.Log("[BattleRewardManager] Created missing UnitProgressManager_AUTO.");
        }

        if (rewardCalculator == null)
            rewardCalculator = FindManager<UnitRewardCalculator>();

        if (cloudSaveManager == null)
            cloudSaveManager = FindManager<ZoidsGameJoltCloudSaveManager>();

        if (perkRewardBridge == null)
            perkRewardBridge = FindManager<ZoidsPerkRewardBridge>();

        if (perkRewardBridge == null)
        {
            GameObject obj = new GameObject("ZoidsPerkRewardBridge_AUTO");
            perkRewardBridge = obj.AddComponent<ZoidsPerkRewardBridge>();

            if (debugLog)
                Debug.Log("[BattleRewardManager] Created ZoidsPerkRewardBridge_AUTO.");
        }

        if (debugLog)
        {
            if (progressManager == null)
                Debug.LogWarning("[BattleRewardManager] UnitProgressManager missing. Reward cannot be saved.");

            if (rewardCalculator == null)
                Debug.LogWarning("[BattleRewardManager] UnitRewardCalculator missing. Using fallback reward amount.");

            if (perkRewardBridge == null)
                Debug.LogWarning("[BattleRewardManager] ZoidsPerkRewardBridge missing.");
        }
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }



    private void UploadCloudSaveAfterReward()
    {
        if (!uploadCloudSaveAfterReward)
            return;

        RefreshRuntimeReferences();

        if (cloudSaveManager == null)
        {
            if (debugLog)
                Debug.LogWarning("[BattleRewardManager] Cloud save manager missing. Battle reward saved locally only.");
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            if (debugLog)
                Debug.LogWarning("[BattleRewardManager] Cloud save manager busy. Battle reward upload skipped.");
            return;
        }

        cloudSaveManager.UploadLocalSaveToCloud();

        if (debugLog)
            Debug.Log("[BattleRewardManager] Uploading battle reward save to Game Jolt.");
    }

    private void ApplyPerkCurrencyReward(BattleResultData result)
    {
        if (result == null)
            return;

        RefreshRuntimeReferences();

        if (perkRewardBridge == null)
            return;

        string battleType = "";
        if (BattleContextManager.Instance != null &&
            BattleContextManager.Instance.HasContext &&
            BattleContextManager.Instance.CurrentContext != null)
        {
            battleType = BattleContextManager.Instance.CurrentContext.battleType;
        }

        int amount = perkRewardBridge.AwardBattlePerkCurrency(result.playerWon, battleType);

        if (debugLog && amount > 0)
            Debug.Log("[BattleRewardManager] Perk currency reward applied. Amount=" + amount + " battleType=" + battleType);
    }

    public BattleResultData BuildResult(bool playerWon, List<int> defeatedEnemyUnitIds)
    {
        BattleResultData result = new BattleResultData();
        result.playerWon = playerWon;

        if (defeatedEnemyUnitIds != null)
            result.defeatedEnemyUnitIds.AddRange(defeatedEnemyUnitIds);

        Dictionary<int, int> totals = new Dictionary<int, int>();

        for (int i = 0; i < result.defeatedEnemyUnitIds.Count; i++)
        {
            int unitId = result.defeatedEnemyUnitIds[i];
            int amount = rewardCalculator != null
                ? rewardCalculator.GetRewardAmount(unitId, playerWon)
                : (playerWon ? 10 : 1);

            if (totals.ContainsKey(unitId))
                totals[unitId] += amount;
            else
                totals.Add(unitId, amount);
        }

        foreach (var kv in totals)
            result.rewards.Add(new BattleRewardUnitData(kv.Key, kv.Value));

        return result;
    }

    public void ApplyResult(BattleResultData result)
    {
        RefreshRuntimeReferences();

        if (result == null)
        {
            Debug.LogWarning("[BattleRewardManager] ApplyResult failed. Result is null.");
            return;
        }

        if (progressManager == null)
        {
            Debug.LogWarning("[BattleRewardManager] ApplyResult failed. UnitProgressManager missing.");
            return;
        }

        for (int i = 0; i < result.rewards.Count; i++)
        {
            BattleRewardUnitData reward = result.rewards[i];
            progressManager.AddUnitData(reward.unitId, reward.dataAmount, false);
        }

        progressManager.SaveProgress();

        ApplyPerkCurrencyReward(result);

        UploadCloudSaveAfterReward();

        if (debugLog)
            Debug.Log("[BattleRewardManager] Reward applied. TotalData=" + result.GetTotalRewardData());
    }
}
