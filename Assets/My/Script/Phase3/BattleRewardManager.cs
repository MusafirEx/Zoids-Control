using System.Collections.Generic;
using UnityEngine;

public class BattleRewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;
    [SerializeField] private UnitRewardCalculator rewardCalculator;

    private void Reset()
    {
        progressManager = FindObjectOfType<UnitProgressManager>();
        rewardCalculator = FindObjectOfType<UnitRewardCalculator>();
    }

    private void Awake()
    {
        if (progressManager == null)
            progressManager = FindObjectOfType<UnitProgressManager>();

        if (rewardCalculator == null)
            rewardCalculator = FindObjectOfType<UnitRewardCalculator>();
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
        if (result == null || progressManager == null)
            return;

        for (int i = 0; i < result.rewards.Count; i++)
        {
            BattleRewardUnitData reward = result.rewards[i];
            progressManager.AddUnitData(reward.unitId, reward.dataAmount, false);
        }

        progressManager.SaveProgress();
    }
}
